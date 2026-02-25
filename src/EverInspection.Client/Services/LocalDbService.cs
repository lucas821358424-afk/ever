using System;
using System.Data.SQLite;
using System.IO;
using EverInspection.Client.Data;

namespace EverInspection.Client.Services
{
    public sealed class LocalDbService
    {
        private const string DbFileName = "ever-inspection.db";
        private const string DbPathEnvName = "EVER_INSPECTION_DB_PATH";

        private readonly string _connectionString;

        public string DbPath { get; }

        public LocalDbService()
        {
            DbPath = ResolveDbPath();
            var dbDirectory = Path.GetDirectoryName(DbPath);
            if (!string.IsNullOrWhiteSpace(dbDirectory))
            {
                Directory.CreateDirectory(dbDirectory);
            }

            _connectionString = $"Data Source={DbPath};Version=3;";

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

        private static string ResolveDbPath()
        {
            var fromEnv = Environment.GetEnvironmentVariable(DbPathEnvName);
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return Path.GetFullPath(fromEnv.Trim());
            }

            var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "EverInspection");
            return Path.Combine(appData, DbFileName);
        }
    }
}
