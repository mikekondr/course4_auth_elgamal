using System.IO;

namespace auth_elgamal.Services
{
    public class LoggingService
    {
        // 1. Singleton: Створюємо єдиний екземпляр сервісу
        public static LoggingService Instance { get; } = new LoggingService();

        private readonly string _logFilePath = "us_book.txt";

        // Об'єкт-заглушка для блокування файлу,
        // щоб уникнути конфліктів, якщо події відбуватимуться одночасно
        private static readonly object _lock = new object();

        // 2. Приватний конструктор, щоб ніхто інший не міг створити екземпляр
        private LoggingService()
        {
            // (Можна додати логіку ініціалізації, якщо потрібно)
        }

        /// <summary>
        /// Головний метод для запису події у журнал.
        /// </summary>
        /// <param name="username">Ім'я користувача (або логін, який намагалися ввести)</param>
        /// <param name="action">Опис події</param>
        public void LogEvent(string username, string action)
        {
            try
            {
                // 3. Форматуємо запис
                string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                string user = username ?? "N/A"; // Обробка, якщо ім'я невідоме
                string logEntry = $"{timestamp} | {user} | {action}";

                // 4. Блокуємо файл для безпечного запису
                lock (_lock)
                {
                    // Дописуємо рядок у кінець файлу
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                // Якщо запис у лог не вдався, ми не хочемо "ламати" програму.
                // Просто виведемо помилку в консоль налагодження.
                System.Diagnostics.Debug.WriteLine($"ПОМИЛКА ЛОГУВАННЯ: {ex.Message}");
            }
        }
    }
}