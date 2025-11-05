using auth_elgamal.Models;
using auth_elgamal.Services;
using System.Windows.Controls;
using System.Windows.Input;

namespace auth_elgamal.ViewModels
{
    public class LoginViewModel : BaseViewModel
    {
        private readonly MainWindowViewModel _mainVM;
        private readonly AuthService _authService;

        private string _username;
        private string _errorMessage;

        public string Username
        {
            get => _username;
            set { _username = value; OnPropertyChanged(); }
        }

        // Властивість для показу помилок
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }

        public LoginViewModel(MainWindowViewModel mainVM)
        {
            _mainVM = mainVM;
            _authService = new AuthService();

            // Ми передаємо параметр (PasswordBox) у команду
            LoginCommand = new RelayCommand(Login, CanLogin);
        }

        // Перевірка, чи можна натиснути кнопку
        private bool CanLogin(object parameter)
        {
            // Кнопка буде активна, тільки якщо їй передано параметр
            return parameter != null;
        }

        private void Login(object parameter)
        {
            ErrorMessage = null; // Скидаємо помилку

            // Отримуємо PasswordBox, переданий як параметр
            var passwordBox = parameter as PasswordBox;
            if (passwordBox == null) return;

            string password = passwordBox.Password;

            // Викликаємо сервіс для валідації
            User user = _authService.ValidateUser(Username, password);

            if (user != null)
            {
                // Успіх! Зберігаємо користувача у головній VM
                LoggingService.Instance.LogEvent(Username, "Успішна авторизація");
                _mainVM.CurrentUser = user;

                // Переходимо на потрібний екран
                if (user.IsAdmin)
                {
                    _mainVM.GoToAdminCommand.Execute(null);
                }
                else
                {
                    _mainVM.GoToUserCommand.Execute(null);
                }

                // Очищуємо поля
                ClearCredentials(passwordBox);
            }
            else
            {
                // Помилка
                LoggingService.Instance.LogEvent(Username, "Помилка авторизації (невірний логін або пароль)");
                ErrorMessage = "Невірний логін або пароль.";
            }
        }

        private void ClearCredentials(PasswordBox passwordBox)
        {
            Username = string.Empty;
            passwordBox.Password = string.Empty;
        }
    }
}