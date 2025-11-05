using auth_elgamal.Services;
using MaterialDesignThemes.Wpf; // Потрібно для Snackbar
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace auth_elgamal.ViewModels.SubViewModels
{
    public class DiskViewModel : BaseViewModel
    {
        // --- Властивості для прив'язки до XAML ---
        public string DiskName { get; set; }
        public bool CanRead { get; set; }
        public bool CanWrite { get; set; }
        public bool CanExecute { get; set; }

        public ObservableCollection<FileViewModel> Files { get; set; }

        // --- Команди ---
        public ICommand CreateFileCommand { get; }
        public ICommand ExecuteCommand { get; }

        // Черга для сповіщень (ми її отримаємо ззовні)
        private readonly ISnackbarMessageQueue _notificationQueue;

        private readonly string _currentUserLogin;

        public DiskViewModel(string diskLetter, string permissions, ISnackbarMessageQueue notificationQueue, string currentUserLogin)
        {
            _notificationQueue = notificationQueue;
            _currentUserLogin = currentUserLogin;

            // 1. Встановлюємо права
            permissions = permissions.ToUpper();
            CanRead = permissions.Contains('R');
            CanWrite = permissions.Contains('W');
            CanExecute = permissions.Contains('E');

            DiskName = $"Диск {diskLetter}: ({permissions})";

            // 2. Ініціалізуємо колекцію файлів
            Files = new ObservableCollection<FileViewModel>();

            // 3. Ініціалізуємо команди
            // Кнопки "Створити" та "Виконати" будуть увімкнені/вимкнені
            // автоматично завдяки IsEnabled у XAML, але ми також 
            // можемо передати 'CanWrite'/'CanExecute' у CanExecute делегат команди.
            CreateFileCommand = new RelayCommand(CreateFile, _ => CanWrite);
            ExecuteCommand = new RelayCommand(Execute, _ => CanExecute);

            // 4. Завантажуємо "уявні" файли, якщо є право 'R'
            if (CanRead)
            {
                LoadDummyFiles(diskLetter, _notificationQueue, _currentUserLogin);
            }
        }

        private void LoadDummyFiles(string diskLetter, ISnackbarMessageQueue _notificationQueue, string _currentUserLogin)
        {
            Files.Add(new FileViewModel($"system_log_{diskLetter}.txt", _notificationQueue, _currentUserLogin));
            Files.Add(new FileViewModel($"config_{diskLetter}.ini", _notificationQueue, _currentUserLogin));
            Files.Add(new FileViewModel($"readme.md", _notificationQueue, _currentUserLogin));
        }

        private void CreateFile(object obj)
        {
            // Імітація: додаємо новий файл до списку
            string newFileName = $"new_file_{Files.Count + 1}.txt";
            Files.Add(new FileViewModel(newFileName, _notificationQueue, _currentUserLogin));

            LoggingService.Instance.LogEvent(_currentUserLogin, $"Створено файл: {newFileName} на диску {DiskName}");

            // Повідомляємо користувача
            _notificationQueue.Enqueue(new Models.Notifications.SuccessNotification
            {
                Message = $"Файл {newFileName} створено на диску {DiskName}."
            });
        }

        private void Execute(object obj)
        {
            LoggingService.Instance.LogEvent(_currentUserLogin, $"Виконано 'Execute' на диску {DiskName}");

            // Імітація: просто показуємо сповіщення
            _notificationQueue.Enqueue(new Models.Notifications.SuccessNotification
            {
                Message = $"Виконання програми на диску {DiskName}..."
            });
        }
    }
}