using System;
using System.Data.SQLite;
using System.IO;
using EverInspection.Client.Data;

namespace EverInspection.Client.Services
{
    public sealed class LocalDbService
    {
        private readonly string _connectionString;

        public LocalDbService()
        {
            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EverInspection");
            Directory.CreateDirectory(appData);
            var dbPath = Path.Combine(appData, "ever-inspection.db");
            _connectionString = $"Data Source={dbPath};Version=3;";

            using (var conn = CreateConnection())
            {
                DatabaseInitializer.Initialize(conn);
            }
        }

        public SQLiteConnection CreateConnection()
        {
            var connection = new SQLiteConnection(_connectionString);
            connection.Open();
            return connection;
        }
    }
}
