using System;
using System.IO;
using auth_elgamal.Models; // Додаємо простір імен моделі

namespace auth_elgamal.Services
{
    public class AuthService
    {
        // Файл буде скопійовано у папку /bin/Debug/
        private readonly string _filePath = "nameuser.txt";

        public User ValidateUser(string login, string password)
        {
            try
            {
                // 1. Перевірка жорстко заданого адміністратора
                if (login == "admin" && password == "admin_123")
                {
                    return new User("admin", true); // Повертаємо admin-користувача
                }

                // 2. Пошук у файлі
                if (!File.Exists(_filePath))
                {
                    // Якщо файлу немає, звичайні користувачі увійти не зможуть
                    return null;
                }

                var lines = File.ReadAllLines(_filePath);
                foreach (var line in lines)
                {
                    var parts = line.Split(':');
                    // Переконуємось, що у рядку 3 частини: login:password:permissions
                    if (parts.Length == 3)
                    {
                        string fileLogin = parts[0];
                        string filePass = parts[1];
                        string filePerms = parts[2];

                        if (fileLogin == login && filePass == password)
                        {
                            // Знайшли! Повертаємо звичайного користувача
                            return new User(fileLogin, filePerms);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Тут можна було б логувати помилку
                // (наприклад, System.Diagnostics.Debug.WriteLine(ex.Message))
            }

            // 3. Якщо нічого не знайдено
            return null;
        }
    }
}