using auth_elgamal.Models.Notifications;
using auth_elgamal.Services;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;

namespace auth_elgamal.ViewModels.SubViewModels
{
    public class FileViewModel : BaseViewModel
    {
        private readonly ISnackbarMessageQueue _notificationQueue;
        private readonly string _currentUserLogin;

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        // Команда для прив'язки до кнопки
        public ICommand ReadFileCommand { get; }

        public FileViewModel(string fileName, ISnackbarMessageQueue notificationQueue, string currentUserLogin)
        {
            FileName = fileName;
            _notificationQueue = notificationQueue;
            _currentUserLogin = currentUserLogin;

            // Ініціалізуємо команду
            ReadFileCommand = new RelayCommand(ReadFile);
        }

        private void ReadFile(object obj)
        {
            LoggingService.Instance.LogEvent(_currentUserLogin, $"Прочитано файл: {FileName}");
            // Імітація читання: відправляємо сповіщення
            _notificationQueue.Enqueue(new SuccessNotification
            {
                Message = $"Прочитано файл: {FileName}"
            });
        }
    }
}