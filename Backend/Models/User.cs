namespace HeimdallBackend.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public virtual Role Role { get; set; } = null!;
        public int RoleId { get; set; }
    }
}