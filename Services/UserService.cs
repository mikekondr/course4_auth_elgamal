using auth_elgamal.ViewModels;
using System.IO;

namespace auth_elgamal.Services
{
    /// <summary>
    /// Сервіс керування користувачами (завантаження та збереження у файл)
    /// </summary>
    public class UserService
    {
        // шлях до файлу з користувачами
        private readonly string _filePath = "nameuser.txt";

        // Завантажує ВСІХ користувачів з файлу
        public List<UserEntryViewModel> LoadUsers()
        {
            var users = new List<UserEntryViewModel>();

            // порожній новий список, якщо файл не існує
            if (!File.Exists(_filePath)) { return users; }

            try
            {
                var lines = File.ReadAllLines(_filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    if (parts.Length == 3)
                    {
                        users.Add(new UserEntryViewModel(parts[0], parts[1], parts[2]));
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading users: {ex.Message}");
            }

            return users;
        }

        // Повністю ПЕРЕЗАПИСУЄ файл користувачів новими даними
        public bool SaveUsers(IEnumerable<UserEntryViewModel> users)
        {
            bool result = false;
            try
            {
                // Конвертація UserEntryViewModel у рядок
                var lines = users.Select(u => $"{u.Login}:{u.Password}:{u.GetPermissionString()}");
                File.WriteAllLines(_filePath, lines);
                result = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving users: {ex.Message}");
            }
            return result;
        }
    }
}