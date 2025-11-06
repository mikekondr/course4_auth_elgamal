using auth_elgamal.Models;

namespace auth_elgamal.Services
{
    /// <summary>
    /// Сервіс авторизації користувачів
    /// </summary>
    public class AuthService
    {
        // залежність від сервісу користувачів
        private readonly UserService _userService;

        public AuthService()
        {
            _userService = new UserService();
        }

        /// <summary>
        /// Перевіряє логін та пароль і повертає об'єкт User в разі успіху.
        /// </summary>
        /// <returns>Об'єкт User (admin або звичайний) або null, якщо валідація не пройдена.</returns>
        public User ValidateUser(string login, string password)
        {
            try
            {
                // Визначені облікові дані адміністратора
                if (login == "admin" && password == "admin_123")
                {
                    return new User("admin", true);
                }

                // Пошук у файлі (через UserService)
                var allUsers = _userService.LoadUsers();

                // Шукаємо співпадіння логіна та пароля
                var foundUserEntry = allUsers.FirstOrDefault(u =>
                    u.Login.Equals(login, StringComparison.Ordinal) &&
                    u.Password == password);

                if (foundUserEntry != null)
                {
                    return new User(foundUserEntry.Login, foundUserEntry.GetPermissionString());
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Auth Error: {ex.Message}");
            }

            // Користувач не знайдений або помилка
            return null;
        }
    }
}