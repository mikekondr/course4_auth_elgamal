using auth_elgamal.Models.Notifications;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;

namespace auth_elgamal.ViewModels.SubViewModels
{
    public class FileViewModel : BaseViewModel
    {
        private readonly ISnackbarMessageQueue _notificationQueue;

        private string _fileName;
        public string FileName
        {
            get => _fileName;
            set { _fileName = value; OnPropertyChanged(); }
        }

        // Команда для прив'язки до кнопки
        public ICommand ReadFileCommand { get; }

        public FileViewModel(string fileName, ISnackbarMessageQueue notificationQueue)
        {
            FileName = fileName;
            _notificationQueue = notificationQueue;

            // Ініціалізуємо команду
            ReadFileCommand = new RelayCommand(ReadFile);
        }

        private void ReadFile(object obj)
        {
            // Імітація читання: відправляємо сповіщення
            _notificationQueue.Enqueue(new SuccessNotification
            {
                Message = $"Прочитано файл: {FileName}"
            });
        }
    }
}