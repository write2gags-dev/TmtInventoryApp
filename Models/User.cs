namespace TmtInventoryApp.Models
{
    public class User
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty; // "Admin" or "Operator"
        public string DisplayName { get; set; } = string.Empty;
    }

    public static class UserRoles
    {
        public const string Admin = "Admin";
        public const string Operator = "Operator";
    }
}
