using System.IO;

namespace auth_elgamal.Services
{
    /// <summary>
    /// Статичний сервіс для логування подій у файл
    /// </summary>
    public class LoggingService
    {
        // Singleton: єдиний екземпляр сервісу
        public static LoggingService Instance { get; } = new LoggingService();

        // Шлях до файлу журналу
        private readonly string _logFilePath = "us_book.txt";

        // Об'єкт-заглушка для блокування файлу,
        // щоб уникнути конфліктів, якщо події відбуватимуться одночасно
        private static readonly object _lock = new object();

        // Приватний конструктор, щоб ніхто інший не міг створити екземпляр
        private LoggingService() { }

        /// <summary>
        /// Головний метод для запису події у журнал.
        /// </summary>
        /// <param name="username">Ім'я користувача (або логін, який намагалися ввести)</param>
        /// <param name="action">Опис події</param>
        public void LogEvent(string username, string action)
        {
            try
            {
                string timestamp = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss");
                string user = username ?? "N/A";
                string logEntry = $"{timestamp} | {user} | {action}";

                lock (_lock)
                {
                    File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Logging error: {ex.Message}");
            }
        }
    }
}