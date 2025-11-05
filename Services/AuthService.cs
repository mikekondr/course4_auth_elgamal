using auth_elgamal.Models;

namespace auth_elgamal.Services
{
    public class AuthService
    {
        // AuthService тепер залежить від UserService, щоб отримати дані
        private readonly UserService _userService;

        public AuthService()
        {
            _userService = new UserService(); // Створюємо екземпляр сервісу
        }

        /// <summary>
        /// Перевіряє логін та пароль і повертає об'єкт User в разі успіху.
        /// </summary>
        /// <returns>Об'єкт User (admin або звичайний) або null, якщо валідація не пройдена.</returns>
        public User ValidateUser(string login, string password)
        {
            try
            {
                // 1. Перевірка жорстко заданого адміністратора
                if (login == "admin" && password == "admin_123")
                {
                    return new User("admin", true); // Повертаємо admin-користувача
                }

                // 2. Пошук у файлі (через UserService)
                // Отримуємо список всіх користувачів (у вигляді UserEntryViewModel)
                var allUsers = _userService.LoadUsers();

                // Шукаємо співпадіння логіна та пароля
                var foundUserEntry = allUsers.FirstOrDefault(u =>
                    u.Login.Equals(login, StringComparison.Ordinal) &&
                    u.Password == password);

                if (foundUserEntry != null)
                {
                    // Знайшли!
                    // Конвертуємо UserEntryViewModel назад у модель User,
                    // яку очікує наша MainWindowViewModel.
                    return new User(foundUserEntry.Login, foundUserEntry.GetPermissionString());
                }
            }
            catch (Exception ex)
            {
                // Тут можна логувати помилку (наприклад, якщо UserService не зміг прочитати файл)
                System.Diagnostics.Debug.WriteLine($"Auth Error: {ex.Message}");
            }

            // 3. Якщо нічого не знайдено
            return null;
        }
    }
}