using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Data.SQLite;
using System.Linq;
using EverInspection.Client.Models;
using Newtonsoft.Json;

namespace EverInspection.Client.Services
{
    public sealed class FormService
    {
        private readonly LocalDbService _db;

        public FormService(LocalDbService db)
        {
            _db = db;
        }

        public FormInstance CreateDraft(string templateId, string templateVersion, string userId, IDictionary<string, string> headerValues, ObservableCollection<DynamicRowItem> rows)
        {
            var instance = new FormInstance
            {
                FormInstanceId = Guid.NewGuid().ToString("N"),
                TemplateId = templateId,
                TemplateVersion = templateVersion,
                OperatorId = userId,
                Status = FormStatus.Draft,
                HasOutOfSpec = rows.Any(x => x.IsOutOfSpec),
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now,
                HeaderValues = headerValues == null ? new Dictionary<string, string>() : new Dictionary<string, string>(headerValues),
                Values = rows.Select(ToCell).ToList()
            };
            Save(instance);
            return instance;
        }

        public void Save(FormInstance instance)
        {
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR REPLACE INTO form_instances
(form_instance_id, template_id, template_version, operator_id, status, has_out_of_spec, header_json, values_json, created_at, updated_at)
VALUES(@id,@templateId,@version,@operator,@status,@outOfSpec,@header,@values,@created,@updated)";
                cmd.Parameters.AddWithValue("@id", instance.FormInstanceId);
                cmd.Parameters.AddWithValue("@templateId", instance.TemplateId);
                cmd.Parameters.AddWithValue("@version", instance.TemplateVersion);
                cmd.Parameters.AddWithValue("@operator", instance.OperatorId);
                cmd.Parameters.AddWithValue("@status", (int)instance.Status);
                cmd.Parameters.AddWithValue("@outOfSpec", instance.HasOutOfSpec ? 1 : 0);
                cmd.Parameters.AddWithValue("@header", JsonConvert.SerializeObject(instance.HeaderValues ?? new Dictionary<string, string>()));
                cmd.Parameters.AddWithValue("@values", JsonConvert.SerializeObject(instance.Values));
                cmd.Parameters.AddWithValue("@created", instance.CreatedAt.ToString("o"));
                cmd.Parameters.AddWithValue("@updated", DateTime.Now.ToString("o"));
                cmd.ExecuteNonQuery();
            }
        }

        public IList<FormInstance> GetInstances(string userId, string templateId = null)
        {
            var result = new List<FormInstance>();
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT form_instance_id, template_id, template_version, operator_id, status, has_out_of_spec, header_json, values_json, created_at, updated_at FROM form_instances WHERE operator_id=@userId";
                if (!string.IsNullOrWhiteSpace(templateId))
                {
                    cmd.CommandText += " AND template_id=@templateId";
                    cmd.Parameters.AddWithValue("@templateId", templateId);
                }

                cmd.CommandText += " ORDER BY created_at DESC";
                cmd.Parameters.AddWithValue("@userId", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new FormInstance
                        {
                            FormInstanceId = reader[0].ToString(),
                            TemplateId = reader[1].ToString(),
                            TemplateVersion = reader[2].ToString(),
                            OperatorId = reader[3].ToString(),
                            Status = (FormStatus)reader.GetInt32(4),
                            HasOutOfSpec = reader.GetInt32(5) == 1,
                            HeaderValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader[6].ToString()) ?? new Dictionary<string, string>(),
                            Values = JsonConvert.DeserializeObject<List<FormCellValue>>(reader[7].ToString()) ?? new List<FormCellValue>(),
                            CreatedAt = DateTime.Parse(reader[8].ToString()),
                            UpdatedAt = DateTime.Parse(reader[9].ToString())
                        });
                    }
                }
            }

            return result;
        }

        public int CountPendingUpload(string templateId)
        {
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT COUNT(1) FROM form_instances WHERE template_id=@templateId AND status=@status";
                cmd.Parameters.AddWithValue("@templateId", templateId);
                cmd.Parameters.AddWithValue("@status", (int)FormStatus.Submitted);
                return Convert.ToInt32(cmd.ExecuteScalar());
            }
        }

        public IList<FormInstance> GetPendingUpload()
        {
            return GetByStatus(FormStatus.Submitted);
        }

        public void Submit(FormInstance instance)
        {
            instance.Status = FormStatus.Submitted;
            instance.UpdatedAt = DateTime.Now;
            Save(instance);
        }

        public void MarkUploaded(string formInstanceId)
        {
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE form_instances SET status=@status, updated_at=@updatedAt WHERE form_instance_id=@id";
                cmd.Parameters.AddWithValue("@status", (int)FormStatus.Uploaded);
                cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("o"));
                cmd.Parameters.AddWithValue("@id", formInstanceId);
                cmd.ExecuteNonQuery();
            }
        }

        private IList<FormInstance> GetByStatus(FormStatus status)
        {
            var result = new List<FormInstance>();
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT form_instance_id, template_id, template_version, operator_id, status, has_out_of_spec, header_json, values_json, created_at, updated_at FROM form_instances WHERE status=@status";
                cmd.Parameters.AddWithValue("@status", (int)status);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new FormInstance
                        {
                            FormInstanceId = reader[0].ToString(),
                            TemplateId = reader[1].ToString(),
                            TemplateVersion = reader[2].ToString(),
                            OperatorId = reader[3].ToString(),
                            Status = (FormStatus)reader.GetInt32(4),
                            HasOutOfSpec = reader.GetInt32(5) == 1,
                            HeaderValues = JsonConvert.DeserializeObject<Dictionary<string, string>>(reader[6].ToString()) ?? new Dictionary<string, string>(),
                            Values = JsonConvert.DeserializeObject<List<FormCellValue>>(reader[7].ToString()) ?? new List<FormCellValue>(),
                            CreatedAt = DateTime.Parse(reader[8].ToString()),
                            UpdatedAt = DateTime.Parse(reader[9].ToString())
                        });
                    }
                }
            }

            return result;
        }

        private static FormCellValue ToCell(DynamicRowItem row)
        {
            return new FormCellValue
            {
                RowIndex = row.RowIndex,
                ValueText = row.Value,
                IsOutOfSpec = row.IsOutOfSpec
            };
        }
    }
}
