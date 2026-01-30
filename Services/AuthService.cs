using TmtInventoryApp.Models;

namespace TmtInventoryApp.Services
{
    public class AuthService
    {
        private static readonly List<User> Users = new()
        {
            new User { Username = "gagan", Password = "admin123", Role = UserRoles.Admin, DisplayName = "Gagan (Owner)" },
            new User { Username = "admin", Password = "admin123", Role = UserRoles.Admin, DisplayName = "Administrator" },
            new User { Username = "sushil", Password = "operator123", Role = UserRoles.Operator, DisplayName = "Sushil" },
            new User { Username = "biku", Password = "operator123", Role = UserRoles.Operator, DisplayName = "Biku" }
        };

        public User? Authenticate(string username, string password)
        {
            return Users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && 
                u.Password == password);
        }

        public static User? GetUserByUsername(string username)
        {
            return Users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase));
        }

        public bool ChangePassword(string username, string currentPassword, string newPassword)
        {
            var user = Users.FirstOrDefault(u => 
                u.Username.Equals(username, StringComparison.OrdinalIgnoreCase) && 
                u.Password == currentPassword);

            if (user != null)
            {
                user.Password = newPassword;
                return true;
            }

            return false;
        }
    }
}
