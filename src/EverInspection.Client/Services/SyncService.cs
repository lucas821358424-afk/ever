using System;
using EverInspection.Client.Models;

namespace EverInspection.Client.Services
{
    public sealed class SyncService
    {
        private readonly FormService _formService;
        private readonly LocalDbService _db;

        public SyncService(FormService formService, LocalDbService db)
        {
            _formService = formService;
            _db = db;
        }

        public int SyncPendingForms(bool networkAvailable)
        {
            if (!networkAvailable)
            {
                WriteLog("Upload", "Failed", "当前离线，无法同步");
                return 0;
            }

            var pending = _formService.GetPendingUpload();
            var count = 0;
            foreach (var item in pending)
            {
                _formService.MarkUploaded(item.FormInstanceId);
                count++;
            }

            WriteLog("Upload", "Success", $"成功上传 {count} 条记录");
            return count;
        }

        private void WriteLog(string type, string result, string message)
        {
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT INTO sync_logs(created_at, type, result, message) VALUES(@time,@type,@result,@message)";
                cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("o"));
                cmd.Parameters.AddWithValue("@type", type);
                cmd.Parameters.AddWithValue("@result", result);
                cmd.Parameters.AddWithValue("@message", message);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
