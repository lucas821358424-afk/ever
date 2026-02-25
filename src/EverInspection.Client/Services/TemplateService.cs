using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
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
                        result.Add(JsonConvert.DeserializeObject<TemplateDefinition>(reader[1].ToString()));
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
                return json == null ? null : JsonConvert.DeserializeObject<TemplateDefinition>(json);
            }
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

                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO templates(template_id, template_name, version, updated_at, content_json)
VALUES(@id,@name,@version,@time,@json)";
                    cmd.Parameters.AddWithValue("@id", template.TemplateId);
                    cmd.Parameters.AddWithValue("@name", template.TemplateName);
                    cmd.Parameters.AddWithValue("@version", template.Version);
                    cmd.Parameters.AddWithValue("@time", template.UpdatedAt.ToString("o"));
                    cmd.Parameters.AddWithValue("@json", json);
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
