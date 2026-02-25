using System.Data.SQLite;

namespace EverInspection.Client.Data
{
    public static class DatabaseInitializer
    {
        public static void Initialize(SQLiteConnection connection)
        {
            using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS users (
    user_id TEXT PRIMARY KEY,
    user_name TEXT NOT NULL,
    password_hash TEXT NOT NULL,
    token TEXT,
    last_login_at TEXT,
    is_locked INTEGER DEFAULT 0
);

CREATE TABLE IF NOT EXISTS permissions (
    user_id TEXT NOT NULL,
    template_id TEXT NOT NULL,
    form_visible INTEGER NOT NULL,
    form_fill INTEGER NOT NULL,
    history_edit INTEGER NOT NULL,
    draft_delete INTEGER NOT NULL,
    PRIMARY KEY(user_id, template_id)
);

CREATE TABLE IF NOT EXISTS templates (
    template_id TEXT NOT NULL,
    template_name TEXT NOT NULL,
    version TEXT NOT NULL,
    updated_at TEXT NOT NULL,
    content_json TEXT NOT NULL,
    PRIMARY KEY(template_id, version)
);

CREATE TABLE IF NOT EXISTS template_header_configs (
    template_id TEXT NOT NULL,
    template_version TEXT NOT NULL,
    key_name TEXT NOT NULL,
    display_name TEXT NOT NULL,
    required INTEGER NOT NULL,
    sort_no INTEGER NOT NULL,
    PRIMARY KEY(template_id, template_version, key_name)
);

CREATE TABLE IF NOT EXISTS template_row_configs (
    template_id TEXT NOT NULL,
    template_version TEXT NOT NULL,
    row_no INTEGER NOT NULL,
    process_name TEXT NOT NULL,
    process_group TEXT,
    item_name TEXT NOT NULL,
    unit TEXT,
    min_value REAL,
    max_value REAL,
    required INTEGER NOT NULL,
    field_type INTEGER NOT NULL,
    PRIMARY KEY(template_id, template_version, row_no)
);

CREATE TABLE IF NOT EXISTS form_instances (
    form_instance_id TEXT PRIMARY KEY,
    template_id TEXT NOT NULL,
    template_version TEXT NOT NULL,
    operator_id TEXT NOT NULL,
    status INTEGER NOT NULL,
    has_out_of_spec INTEGER NOT NULL,
    header_json TEXT NOT NULL DEFAULT '{}',
    values_json TEXT NOT NULL,
    created_at TEXT NOT NULL,
    updated_at TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS sync_logs (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    created_at TEXT NOT NULL,
    type TEXT NOT NULL,
    result TEXT NOT NULL,
    message TEXT
);";
                cmd.ExecuteNonQuery();
            }

            EnsureColumn(connection, "form_instances", "header_json", "TEXT NOT NULL DEFAULT '{}' ");
        }

        private static void EnsureColumn(SQLiteConnection connection, string tableName, string columnName, string typeSql)
        {
            using (var check = connection.CreateCommand())
            {
                check.CommandText = "PRAGMA table_info(" + tableName + ")";
                using (var reader = check.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        if (reader[1].ToString() == columnName)
                        {
                            return;
                        }
                    }
                }
            }

            using (var alter = connection.CreateCommand())
            {
                alter.CommandText = "ALTER TABLE " + tableName + " ADD COLUMN " + columnName + " " + typeSql;
                alter.ExecuteNonQuery();
            }
        }
    }
}
