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
        private readonly Action<bool> _showLoading;

        public DriveViewModel DriveVM { get; }
        public EncryptionViewModel EncryptionVM { get; }

        private BaseViewModel _currentSubViewModel;
        public BaseViewModel CurrentSubViewModel
        {
            get => _currentSubViewModel;
            set
            {
                if (_currentSubViewModel == value) return;
                _currentSubViewModel = value;
                OnPropertyChanged();

                OnPropertyChanged(nameof(SelectedTabIndex));
            }
        }

        public int SelectedTabIndex
        {
            get
            {
                if (CurrentSubViewModel is EncryptionViewModel)
                    return 1;
                return 0;
            }
            set
            {
                if (value == 1)
                    ActivateEncryption(null);
                else
                    ActivateDrives(null);
            }
        }

        private string _welcomeMessage;
        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set { _welcomeMessage = value; OnPropertyChanged(); }
        }

        public User CurrentUser => _mainVM.CurrentUser;

        public ICommand LogoutCommand { get; }

        public UserViewModel(MainWindowViewModel mainVM, ISnackbarMessageQueue notificationQueue, Action<bool> showLoading)
        {
            _mainVM = mainVM;
            _notificationQueue = notificationQueue;
            _showLoading = showLoading;

            LogoutCommand = new RelayCommand(_ => _mainVM.GoToLoginCommand.Execute(null));

            DriveVM = new DriveViewModel(_notificationQueue);
            EncryptionVM = new EncryptionViewModel(_notificationQueue, _showLoading);

            CurrentSubViewModel = DriveVM;
        }

        private void ActivateDrives(object obj)
        {
            LoggingService.Instance.LogEvent(_mainVM.CurrentUser.Login, "Перехід до розділу дисків");

            DriveVM.Activate(_mainVM.CurrentUser);

            CurrentSubViewModel = DriveVM;
        }

        private void ActivateEncryption(object obj)
        {
            LoggingService.Instance.LogEvent(_mainVM.CurrentUser.Login, "Перехід до розділу шифрування");

            EncryptionVM.Activate(_mainVM.CurrentUser);
            CurrentSubViewModel = EncryptionVM;
        }

        public void Activate()
        {
            if (CurrentUser != null)
            {
                WelcomeMessage = $"Привіт, {CurrentUser.Login}!";
            }

            ActivateDrives(null);
        }

        public void ClearSessionData()
        {
            // Очищуємо диски
            DriveVM.ClearData();

            // Очищуємо шифрування
            EncryptionVM.ClearAllData();

            // Скидаємо привітання
            WelcomeMessage = string.Empty;

            // Повертаємо на екран дисків за замовчуванням (для наступного користувача)
            CurrentSubViewModel = DriveVM;
        }
    }
}