using auth_elgamal.Services;
using System.Windows;

namespace auth_elgamal
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Логуємо подію запуску
            LoggingService.Instance.LogEvent("System", "Запуск програми");
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Логуємо подію завершення
            LoggingService.Instance.LogEvent("System", "Завершення роботи програми\r\n");
            base.OnExit(e);
        }
    }

}
