using System;
using System.Data.SQLite;
using System.Security.Cryptography;
using System.Text;
using EverInspection.Client.Models;

namespace EverInspection.Client.Services
{
    public sealed class AuthService
    {
        private readonly LocalDbService _db;

        public AuthService(LocalDbService db)
        {
            _db = db;
            EnsureSeedUser();
        }

        public UserInfo Login(string userId, string password, bool isOnline)
        {
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "SELECT user_id, user_name, password_hash, token, is_locked FROM users WHERE user_id = @userId";
                cmd.Parameters.AddWithValue("@userId", userId);
                using (var reader = cmd.ExecuteReader())
                {
                    if (!reader.Read())
                    {
                        return null;
                    }

                    if (Convert.ToInt32(reader[4]) == 1)
                    {
                        throw new InvalidOperationException("账号已锁定");
                    }

                    var storedHash = reader[2].ToString();
                    if (storedHash != Hash(password))
                    {
                        return null;
                    }

                    if (!isOnline && string.IsNullOrWhiteSpace(reader[3].ToString()))
                    {
                        throw new InvalidOperationException("首次登录必须联网");
                    }

                    var token = Guid.NewGuid().ToString("N");
                    UpdateLoginState(conn, userId, token);

                    return new UserInfo
                    {
                        UserId = reader[0].ToString(),
                        UserName = reader[1].ToString(),
                        Token = token,
                        LastLoginAt = DateTime.Now
                    };
                }
            }
        }

        private static string Hash(string raw)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
                return Convert.ToBase64String(bytes);
            }
        }

        private void EnsureSeedUser()
        {
            using (var conn = _db.CreateConnection())
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "INSERT OR IGNORE INTO users(user_id, user_name, password_hash, token, last_login_at, is_locked) VALUES('operator01','操作员A',@hash,'','',0)";
                cmd.Parameters.AddWithValue("@hash", Hash("123456"));
                cmd.ExecuteNonQuery();
            }
        }

        private static void UpdateLoginState(SQLiteConnection conn, string userId, string token)
        {
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "UPDATE users SET token=@token, last_login_at=@time WHERE user_id=@userId";
                cmd.Parameters.AddWithValue("@token", token);
                cmd.Parameters.AddWithValue("@time", DateTime.Now.ToString("o"));
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
