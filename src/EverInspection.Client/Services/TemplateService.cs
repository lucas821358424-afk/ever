using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using EverInspection.Client.Models;
using Newtonsoft.Json;

namespace EverInspection.Client.Services
{
    public sealed class TemplateService
    {
        private readonly LocalDbService _db;

        public TemplateService(LocalDbService db)
        {
            _db = db;
            EnsureSeedTemplate();
        }

        public IList<TemplateDefinition> GetLatestTemplates()
        {
            var result = new List<TemplateDefinition>();
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"SELECT t1.template_id, t1.content_json
FROM templates t1
INNER JOIN (
    SELECT template_id, MAX(updated_at) max_updated_at
    FROM templates
    GROUP BY template_id
) t2 ON t1.template_id = t2.template_id AND t1.updated_at = t2.max_updated_at";
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var template = JsonConvert.DeserializeObject<TemplateDefinition>(reader[1].ToString());
                        LoadConfigRows(conn, template);
                        result.Add(template);
                    }
                }
            }

            return result;
        }

        public TemplateDefinition GetTemplateByVersion(string templateId, string version)
        {
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT content_json FROM templates WHERE template_id=@id AND version=@version";
                cmd.Parameters.AddWithValue("@id", templateId);
                cmd.Parameters.AddWithValue("@version", version);
                var json = cmd.ExecuteScalar() as string;
                if (json == null)
                {
                    return null;
                }

                var template = JsonConvert.DeserializeObject<TemplateDefinition>(json);
                LoadConfigRows(conn, template);
                return template;
            }
        }

        public int SyncTemplateConfigsFromOracle(bool networkAvailable)
        {
            if (!networkAvailable)
            {
                return 0;
            }

            // 模拟在线时从服务器（Oracle）更新模板配置：优先读取本地可替换的快照文件。
            var snapshotPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "oracle-template-sync.json");
            if (!File.Exists(snapshotPath))
            {
                return 0;
            }

            var json = File.ReadAllText(snapshotPath);
            var templates = JsonConvert.DeserializeObject<List<TemplateDefinition>>(json) ?? new List<TemplateDefinition>();
            using (var conn = _db.CreateConnection())
            {
                foreach (var template in templates)
                {
                    UpsertTemplate(conn, template, JsonConvert.SerializeObject(template));
                    UpsertTemplateConfig(conn, template);
                }
            }

            return templates.Count;
        }

        private void EnsureSeedTemplate()
        {
            using (var conn = _db.CreateConnection())
            using (var check = conn.CreateCommand())
            {
                check.CommandText = "SELECT COUNT(1) FROM templates";
                var count = Convert.ToInt32(check.ExecuteScalar());
                if (count > 0)
                {
                    return;
                }

                var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "sample-template.json");
                var json = File.ReadAllText(path);
                var template = JsonConvert.DeserializeObject<TemplateDefinition>(json);

                UpsertTemplate(conn, template, json);
                UpsertTemplateConfig(conn, template);
            }
        }

        private static void UpsertTemplate(SQLiteConnection conn, TemplateDefinition template, string rawJson)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO templates(template_id, template_name, version, updated_at, content_json)
VALUES(@id,@name,@version,@time,@json)";
                cmd.Parameters.AddWithValue("@id", template.TemplateId);
                cmd.Parameters.AddWithValue("@name", template.TemplateName);
                cmd.Parameters.AddWithValue("@version", template.Version);
                cmd.Parameters.AddWithValue("@time", template.UpdatedAt.ToString("o"));
                cmd.Parameters.AddWithValue("@json", rawJson);
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpsertTemplateConfig(SQLiteConnection conn, TemplateDefinition template)
        {
            using (var deleteHeader = conn.CreateCommand())
            {
                deleteHeader.CommandText = "DELETE FROM template_header_configs WHERE template_id=@id AND template_version=@version";
                deleteHeader.Parameters.AddWithValue("@id", template.TemplateId);
                deleteHeader.Parameters.AddWithValue("@version", template.Version);
                deleteHeader.ExecuteNonQuery();
            }

            using (var deleteRows = conn.CreateCommand())
            {
                deleteRows.CommandText = "DELETE FROM template_row_configs WHERE template_id=@id AND template_version=@version";
                deleteRows.Parameters.AddWithValue("@id", template.TemplateId);
                deleteRows.Parameters.AddWithValue("@version", template.Version);
                deleteRows.ExecuteNonQuery();
            }

            for (var i = 0; i < template.HeaderFields.Count; i++)
            {
                var header = template.HeaderFields[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO template_header_configs(template_id, template_version, key_name, display_name, required, sort_no)
VALUES(@templateId,@version,@key,@display,@required,@sortNo)";
                    cmd.Parameters.AddWithValue("@templateId", template.TemplateId);
                    cmd.Parameters.AddWithValue("@version", template.Version);
                    cmd.Parameters.AddWithValue("@key", header.Key);
                    cmd.Parameters.AddWithValue("@display", header.DisplayName);
                    cmd.Parameters.AddWithValue("@required", header.Required ? 1 : 0);
                    cmd.Parameters.AddWithValue("@sortNo", i + 1);
                    cmd.ExecuteNonQuery();
                }
            }

            for (var i = 0; i < template.Rows.Count; i++)
            {
                var row = template.Rows[i];
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO template_row_configs(template_id, template_version, row_no, process_name, process_group, item_name, unit, min_value, max_value, required, field_type)
VALUES(@templateId,@version,@rowNo,@process,@processGroup,@item,@unit,@min,@max,@required,@fieldType)";
                    cmd.Parameters.AddWithValue("@templateId", template.TemplateId);
                    cmd.Parameters.AddWithValue("@version", template.Version);
                    cmd.Parameters.AddWithValue("@rowNo", i + 1);
                    cmd.Parameters.AddWithValue("@process", row.Process ?? string.Empty);
                    cmd.Parameters.AddWithValue("@processGroup", row.ProcessGroup ?? string.Empty);
                    cmd.Parameters.AddWithValue("@item", row.ItemName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@unit", row.Unit ?? string.Empty);
                    cmd.Parameters.AddWithValue("@min", row.Min.HasValue ? (object)row.Min.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@max", row.Max.HasValue ? (object)row.Max.Value : DBNull.Value);
                    cmd.Parameters.AddWithValue("@required", row.Required ? 1 : 0);
                    cmd.Parameters.AddWithValue("@fieldType", (int)row.FieldType);
                    cmd.ExecuteNonQuery();
                }
            }
        }

        private static void LoadConfigRows(SQLiteConnection conn, TemplateDefinition template)
        {
            template.HeaderFields = new List<HeaderFieldDefinition>();
            template.Rows = new List<TemplateRowDefinition>();

            using (var headerCmd = conn.CreateCommand())
            {
                headerCmd.CommandText = @"SELECT key_name, display_name, required
FROM template_header_configs
WHERE template_id=@id AND template_version=@version
ORDER BY sort_no";
                headerCmd.Parameters.AddWithValue("@id", template.TemplateId);
                headerCmd.Parameters.AddWithValue("@version", template.Version);
                using (var reader = headerCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        template.HeaderFields.Add(new HeaderFieldDefinition
                        {
                            Key = reader[0].ToString(),
                            DisplayName = reader[1].ToString(),
                            Required = reader.GetInt32(2) == 1
                        });
                    }
                }
            }

            using (var rowCmd = conn.CreateCommand())
            {
                rowCmd.CommandText = @"SELECT process_name, process_group, item_name, unit, min_value, max_value, required, field_type
FROM template_row_configs
WHERE template_id=@id AND template_version=@version
ORDER BY row_no";
                rowCmd.Parameters.AddWithValue("@id", template.TemplateId);
                rowCmd.Parameters.AddWithValue("@version", template.Version);
                using (var reader = rowCmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        template.Rows.Add(new TemplateRowDefinition
                        {
                            Process = reader[0].ToString(),
                            ProcessGroup = reader[1].ToString(),
                            ItemName = reader[2].ToString(),
                            Unit = reader[3].ToString(),
                            Min = reader.IsDBNull(4) ? (decimal?)null : Convert.ToDecimal(reader.GetDouble(4)),
                            Max = reader.IsDBNull(5) ? (decimal?)null : Convert.ToDecimal(reader.GetDouble(5)),
                            Required = reader.GetInt32(6) == 1,
                            FieldType = (FieldType)reader.GetInt32(7)
                        });
                    }
                }
            }

            if (template.EqIdOptions == null || !template.EqIdOptions.Any())
            {
                template.EqIdOptions = new List<string> { "EQ-001", "EQ-002", "EQ-003" };
            }

            if (template.MachineTypeOptions == null || !template.MachineTypeOptions.Any())
            {
                template.MachineTypeOptions = new List<string> { "A机种", "B机种", "C机种" };
            }
        }
    }
}
