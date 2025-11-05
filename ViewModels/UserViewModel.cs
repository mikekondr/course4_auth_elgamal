using auth_elgamal.Models;
using auth_elgamal.Services;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;

namespace auth_elgamal.ViewModels
{
    public class UserViewModel : BaseViewModel
    {
        private readonly MainWindowViewModel _mainVM;
        private readonly ISnackbarMessageQueue _notificationQueue;

        private BaseViewModel _currentSubViewModel; // Поточний обраний режим

        private readonly DriveViewModel _driveVM;
        private readonly EncryptionViewModel _encryptionVM;

        public BaseViewModel CurrentSubViewModel
        {
            get => _currentSubViewModel;
            set { _currentSubViewModel = value; OnPropertyChanged(); }
        }

        // Команди для перемикання (будуть прив'язані до кнопок меню)
        public ICommand GoToDrivesCommand { get; }
        public ICommand GoToEncryptionCommand { get; }

        private string _welcomeMessage;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set { _welcomeMessage = value; OnPropertyChanged(); }
        }

        // Властивість для прямого доступу до користувача (якщо потрібно)
        public User CurrentUser => _mainVM.CurrentUser;

        public ICommand LogoutCommand { get; }

        public UserViewModel(MainWindowViewModel mainVM, ISnackbarMessageQueue notificationQueue)
        {
            _mainVM = mainVM;
            _notificationQueue = notificationQueue;
            LogoutCommand = new RelayCommand(_ => _mainVM.GoToLoginCommand.Execute(null));

            // Ініціалізуємо наші під-VM
            _driveVM = new DriveViewModel(_notificationQueue);
            _encryptionVM = new EncryptionViewModel();

            // Ініціалізуємо команди
            GoToDrivesCommand = new RelayCommand(ActivateDrives);
            GoToEncryptionCommand = new RelayCommand(_ => CurrentSubViewModel = _encryptionVM);

            // Встановлюємо режим за замовчуванням (наприклад, диски)
            CurrentSubViewModel = _driveVM;
        }

        private void ActivateDrives(object obj)
        {
            LoggingService.Instance.LogEvent(_mainVM.CurrentUser.Login, "Перехід до розділу дисків");

            // 1. Активуємо VM, передаючи їй поточного користувача
            _driveVM.Activate(_mainVM.CurrentUser);

            // 2. Встановлюємо її як поточний інтерфейс
            CurrentSubViewModel = _driveVM;
        }

        // Цей метод викликається з MainWindowViewModel перед показом
        public void Activate()
        {
            if (CurrentUser != null)
            {
                WelcomeMessage = $"Привіт, {CurrentUser.Login}!";
            }

            ActivateDrives(null);
        }
    }
}