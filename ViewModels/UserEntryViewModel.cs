using System.Text;

namespace auth_elgamal.ViewModels
{
    // Цей клас представляє ОДИН РЯДОК у файлі nameuser.txt
    // і використовується для прив'язки до DataGrid та полів редагування
    public class UserEntryViewModel : BaseViewModel
    {
        private string _login;
        private string _password;
        private bool _loginIsEditable = true;

        public string Login
        {
            get => _login;
            set { _login = value; OnPropertyChanged(); }
        }

        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        // Для блокування редагування логіну існуючого користувача
        public bool LoginIsEditable
        {
            get => _loginIsEditable;
            set { _loginIsEditable = value; OnPropertyChanged(); }
        }

        // --- Матриця Доступу (9 прапорців) ---
        // Диск A
        private bool _diskA_R;
        private bool _diskA_W;
        private bool _diskA_E;
        public bool DiskA_R { get => _diskA_R; set { _diskA_R = value; OnPropertyChanged(); OnPropertyChanged(nameof(PermissionSummary)); } }
        public bool DiskA_W { get => _diskA_W; set { _diskA_W = value; OnPropertyChanged(); OnPropertyChanged(nameof(PermissionSummary)); } }
        public bool DiskA_E { get => _diskA_E; set { _diskA_E = value; OnPropertyChanged(); OnPropertyChanged(nameof(PermissionSummary)); } }

        // Диск B
        private bool _diskB_R;
        private bool _diskB_W;
        private bool _diskB_E;
        public bool DiskB_R { get => _diskB_R; set { _diskB_R = value; OnPropertyChanged(); OnPropertyChanged(nameof(PermissionSummary)); } }
        public bool DiskB_W { get => _diskB_W; set { _diskB_W = value; OnPropertyChanged(); OnPropertyChanged(nameof(PermissionSummary)); } }
        public bool DiskB_E { get => _diskB_E; set { _diskB_E = value; OnPropertyChanged(); OnPropertyChanged(nameof(PermissionSummary)); } }

        // Диск C
        private bool _diskC_R;
        private bool _diskC_W;
        private bool _diskC_E;
        public bool DiskC_R { get => _diskC_R; set { _diskC_R = value; OnPropertyChanged(); OnPropertyChanged(nameof(PermissionSummary)); } }
        public bool DiskC_W { get => _diskC_W; set { _diskC_W = value; OnPropertyChanged(); OnPropertyChanged(nameof(PermissionSummary)); } }
        public bool DiskC_E { get => _diskC_E; set { _diskC_E = value; OnPropertyChanged(); OnPropertyChanged(nameof(PermissionSummary)); } }


        // --- Конструктори та Методи ---

        // Конструктор для нового, порожнього користувача
        public UserEntryViewModel() { }

        // Конструктор для існуючого користувача (з файлу)
        public UserEntryViewModel(string login, string password, string permString)
        {
            Login = login;
            Password = password;
            LoginIsEditable = false; // Логін існуючого користувача редагувати не можна
            ParsePermissions(permString);
        }

        // Властивість для колонки "Permissions" у DataGrid
        public string PermissionSummary => GetPermissionString();

        // Розбирає рядок "A=RWE,C=R" у 9 прапорців
        private void ParsePermissions(string permString)
        {
            if (string.IsNullOrEmpty(permString)) return;

            var pairs = permString.ToUpper().Split(','); // ["A=RWE", "C=R"]
            foreach (var pair in pairs)
            {
                var parts = pair.Split('='); // ["A", "RWE"]
                if (parts.Length != 2) continue;

                string disk = parts[0];
                string rights = parts[1];

                if (disk == "A")
                {
                    DiskA_R = rights.Contains("R");
                    DiskA_W = rights.Contains("W");
                    DiskA_E = rights.Contains("E");
                }
                else if (disk == "B")
                {
                    DiskB_R = rights.Contains("R");
                    DiskB_W = rights.Contains("W");
                    DiskB_E = rights.Contains("E");
                }
                else if (disk == "C")
                {
                    DiskC_R = rights.Contains("R");
                    DiskC_W = rights.Contains("W");
                    DiskC_E = rights.Contains("E");
                }
            }
        }

        // Збирає 9 прапорців назад у рядок "A=RWE,C=R"
        public string GetPermissionString()
        {
            var sb = new StringBuilder();

            // Диск A
            string aPerms = (DiskA_R ? "R" : "") + (DiskA_W ? "W" : "") + (DiskA_E ? "E" : "");
            if (aPerms.Length > 0) sb.Append($"A={aPerms},");

            // Диск B
            string bPerms = (DiskB_R ? "R" : "") + (DiskB_W ? "W" : "") + (DiskB_E ? "E" : "");
            if (bPerms.Length > 0) sb.Append($"B={bPerms},");

            // Диск C
            string cPerms = (DiskC_R ? "R" : "") + (DiskC_W ? "W" : "") + (DiskC_E ? "E" : "");
            if (cPerms.Length > 0) sb.Append($"C={cPerms},");

            return sb.ToString().TrimEnd(','); // Видаляємо останню кому
        }

        public void UpdateFrom(UserEntryViewModel source)
        {
            // Ми НЕ оновлюємо Login, оскільки це наш "ключ"
            this.Password = source.Password;

            this.DiskA_R = source.DiskA_R;
            this.DiskA_W = source.DiskA_W;
            this.DiskA_E = source.DiskA_E;

            this.DiskB_R = source.DiskB_R;
            this.DiskB_W = source.DiskB_W;
            this.DiskB_E = source.DiskB_E;

            this.DiskC_R = source.DiskC_R;
            this.DiskC_W = source.DiskC_W;
            this.DiskC_E = source.DiskC_E;

            // OnPropertyChanged(nameof(PermissionSummary)) 
            // спрацює автоматично завдяки сеттерам окремих властивостей.
        }
    }
}