using auth_elgamal.Models;
using System.Windows.Input;
using MaterialDesignThemes.Wpf;

namespace auth_elgamal.ViewModels
{
    public class UserViewModel : BaseViewModel
    {
        private readonly MainWindowViewModel _mainVM;
        private readonly ISnackbarMessageQueue _notificationQueue;

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
        }

        // Цей метод викликається з MainWindowViewModel перед показом
        public void Activate()
        {
            if (CurrentUser != null)
            {
                WelcomeMessage = $"Вітаємо, {CurrentUser.Login}!";
            }
        }
    }
}