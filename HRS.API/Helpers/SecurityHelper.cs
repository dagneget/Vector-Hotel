using BCrypt.Net;

namespace HRS.API.Helpers
{
    public static class SecurityHelper
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password);
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrEmpty(hashedPassword)) return false;
            
            try
            {
                // Check if it looks like a BCrypt hash (starts with $2)
                if (hashedPassword.StartsWith("$2"))
                {
                    return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
                }
            }
            catch { }

            // Fallback for plain-text or malformed hashes
            return password == hashedPassword;
        }
    }
}
