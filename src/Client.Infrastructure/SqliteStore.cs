using Client.Domain;
using Microsoft.Data.Sqlite;
using System.Text.Json;

namespace Client.Infrastructure;

public sealed class SqliteStore
{
    private readonly string _connStr;

    public SqliteStore(string dbPath)
    {
        _connStr = $"Data Source={dbPath}";
    }

    public void Initialize()
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
CREATE TABLE IF NOT EXISTS form_template (
  template_id TEXT NOT NULL,
  version TEXT NOT NULL,
  name TEXT NOT NULL,
  background_path TEXT NOT NULL,
  schema_json TEXT NOT NULL,
  PRIMARY KEY(template_id, version)
);

CREATE TABLE IF NOT EXISTS form_record (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  template_id TEXT NOT NULL,
  version TEXT NOT NULL,
  user_id TEXT NOT NULL,
  fill_time TEXT NOT NULL,
  upload_status INTEGER NOT NULL DEFAULT 0,
  upload_time TEXT
);

CREATE TABLE IF NOT EXISTS field_data (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  record_id INTEGER NOT NULL,
  field_id TEXT NOT NULL,
  value TEXT,
  FOREIGN KEY(record_id) REFERENCES form_record(id)
);";
        cmd.ExecuteNonQuery();
    }

    public void SaveTemplate(FormTemplate template)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"INSERT OR REPLACE INTO form_template(template_id, version, name, background_path, schema_json)
                            VALUES($id, $ver, $name, $bg, $schema);";
        cmd.Parameters.AddWithValue("$id", template.TemplateId);
        cmd.Parameters.AddWithValue("$ver", template.Version);
        cmd.Parameters.AddWithValue("$name", template.Name);
        cmd.Parameters.AddWithValue("$bg", template.BackgroundImage);
        cmd.Parameters.AddWithValue("$schema", JsonSerializer.Serialize(template.Fields));
        cmd.ExecuteNonQuery();
    }

    public FormTemplate? LoadTemplate(string templateId)
    {
        using var conn = new SqliteConnection(_connStr);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"SELECT template_id, version, name, background_path, schema_json
                            FROM form_template WHERE template_id = $id ORDER BY version DESC LIMIT 1";
        cmd.Parameters.AddWithValue("$id", templateId);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        var fields = JsonSerializer.Deserialize<List<FormField>>(reader.GetString(4)) ?? new List<FormField>();
        return new FormTemplate(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), fields);
    }
}
