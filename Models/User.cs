namespace auth_elgamal.Models
{
    public class User
    {
        public string Login { get; set; }
        public bool IsAdmin { get; set; } = false;

        // Key: Drive Letter (e.g., "A"), Value: Permissions (e.g., "RWE")
        public Dictionary<string, string> Permissions { get; private set; }
            = new Dictionary<string, string>();

        // Конструктор для звичайних користувачів
        public User(string login, string permString)
        {
            Login = login;
            ParsePermissions(permString);
        }

        // Конструктор для адміністратора
        public User(string login, bool isAdmin)
        {
            Login = login;
            IsAdmin = isAdmin;
        }

        private void ParsePermissions(string permString)
        {
            if (string.IsNullOrEmpty(permString)) return;

            // permString = "A=RWE,C=RW"
            var pairs = permString.Split(','); // ["A=RWE", "C=RW"]
            foreach (var pair in pairs)
            {
                var parts = pair.Split('='); // ["A", "RWE"]
                if (parts.Length == 2)
                {
                    // Додаємо у словник: Key="A", Value="RWE"
                    Permissions[parts[0].ToUpper()] = parts[1].ToUpper();
                }
            }
        }
    }
}