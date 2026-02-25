using System.Collections.Generic;
using System.Data.SQLite;
using EverInspection.Client.Models;

namespace EverInspection.Client.Services
{
    public sealed class PermissionService
    {
        private readonly LocalDbService _db;

        public PermissionService(LocalDbService db)
        {
            _db = db;
            EnsureSeedPermission();
        }

        public IDictionary<string, PermissionInfo> GetPermissionsByUser(string userId)
        {
            var dict = new Dictionary<string, PermissionInfo>();
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT template_id, form_visible, form_fill, history_edit, draft_delete FROM permissions WHERE user_id=@uid";
                cmd.Parameters.AddWithValue("@uid", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        dict[reader[0].ToString()] = new PermissionInfo
                        {
                            UserId = userId,
                            TemplateId = reader[0].ToString(),
                            FormVisible = reader.GetInt32(1) == 1,
                            FormFill = reader.GetInt32(2) == 1,
                            HistoryEdit = reader.GetInt32(3) == 1,
                            DraftDelete = reader.GetInt32(4) == 1
                        };
                    }
                }
            }

            return dict;
        }

        private void EnsureSeedPermission()
        {
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT OR IGNORE INTO permissions(user_id, template_id, form_visible, form_fill, history_edit, draft_delete)
VALUES('operator01','TEMP-001',1,1,0,1)";
                cmd.ExecuteNonQuery();
            }
        }
    }
}
