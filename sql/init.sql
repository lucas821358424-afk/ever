CREATE TABLE form_record (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  template_id TEXT NOT NULL,
  version TEXT NOT NULL,
  user_id TEXT NOT NULL,
  fill_time TEXT NOT NULL,
  upload_status INTEGER NOT NULL DEFAULT 0,
  upload_time TEXT,
  created_at TEXT NOT NULL,
  updated_at TEXT NOT NULL
);

CREATE TABLE field_data (
  id INTEGER PRIMARY KEY AUTOINCREMENT,
  record_id INTEGER NOT NULL,
  field_id TEXT NOT NULL,
  value TEXT,
  FOREIGN KEY(record_id) REFERENCES form_record(id)
);
