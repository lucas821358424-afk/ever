using System;

namespace EverInspection.Client.Models
{
    public sealed class UserInfo
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string PasswordHash { get; set; }
        public string Token { get; set; }
        public DateTime LastLoginAt { get; set; }
        public bool IsLocked { get; set; }
    }
}
