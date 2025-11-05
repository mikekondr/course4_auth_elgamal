using auth_elgamal.ViewModels; // Потрібно для UserEntryViewModel
using System.IO;

namespace auth_elgamal.Services
{
    public class UserService
    {
        private readonly string _filePath = "nameuser.txt";

        // Завантажує ВСІХ користувачів з файлу (окрім admin)
        public List<UserEntryViewModel> LoadUsers()
        {
            var users = new List<UserEntryViewModel>();
            if (!File.Exists(_filePath))
            {
                return users; // Повертаємо порожній список
            }

            try
            {
                var lines = File.ReadAllLines(_filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    if (parts.Length == 3)
                    {
                        // Створюємо VM для редагування
                        users.Add(new UserEntryViewModel(parts[0], parts[1], parts[2]));
                    }
                }
            }
            catch (Exception ex)
            {
                // Обробка помилки читання файлу
                System.Diagnostics.Debug.WriteLine($"Error loading users: {ex.Message}");
            }
            return users;
        }

        // Повністю ПЕРЕЗАПИСУЄ файл новими даними
        public bool SaveUsers(IEnumerable<UserEntryViewModel> users)
        {
            try
            {
                // Конвертуємо UserEntryViewModel назад у рядки
                var lines = users.Select(u =>
                    $"{u.Login}:{u.Password}:{u.GetPermissionString()}"
                );

                File.WriteAllLines(_filePath, lines);
                return true;
            }
            catch (Exception ex)
            {
                // Обробка помилки запису
                System.Diagnostics.Debug.WriteLine($"Error saving users: {ex.Message}");
                return false;
            }
        }
    }
}