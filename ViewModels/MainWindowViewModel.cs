using auth_elgamal.Models;
using auth_elgamal.Services;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;

namespace auth_elgamal.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private BaseViewModel _currentViewModel;
        private User _currentUser;

        private readonly LoginViewModel _loginVM;
        private readonly AdminViewModel _adminVM;
        private readonly UserViewModel _userVM;

        public ISnackbarMessageQueue NotificationQueue { get; }

        // Властивість, до якої буде прив'язаний ContentControl
        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set
            {
                _currentViewModel = value;
                OnPropertyChanged();
            }
        }

        // Властивість для зберігання поточного користувача
        public User CurrentUser
        {
            get => _currentUser;
            set
            {
                _currentUser = value;
                OnPropertyChanged();
            }
        }

        // Команди для навігації (для прикладу)
        // У реальному додатку вони б викликалися з дочірніх VM
        public ICommand GoToLoginCommand { get; }
        public ICommand GoToAdminCommand { get; }
        public ICommand GoToUserCommand { get; }

        public MainWindowViewModel()
        {
            // 3. Ініціалізуйте чергу (тут: 3 секунди на кожне повідомлення)
            NotificationQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));

            // Створюємо екземпляри VM для кожного режиму
            // Передаємо 'this', щоб дочірні VM могли викликати навігацію
            _loginVM = new LoginViewModel(this); // Помилки логіну краще залишити inline
            _adminVM = new AdminViewModel(this, NotificationQueue);
            _userVM = new UserViewModel(this, NotificationQueue); // На майбутнє

            // При переході на AdminView...
            GoToAdminCommand = new RelayCommand(_ => CurrentViewModel = _adminVM);

            // При переході на UserView...
            GoToUserCommand = new RelayCommand(_ =>
            {
                // ...ми "активуємо" UserVM, щоб вона оновила дані
                _userVM.Activate();
                CurrentViewModel = _userVM;
            });

            // При виході (GoToLogin) - очищуємо сесію
            GoToLoginCommand = new RelayCommand(_ =>
            {
                LoggingService.Instance.LogEvent(CurrentUser.Login, "Вихід із системи");
                CurrentUser = null; // <-- ОЧИЩЕННЯ
                CurrentViewModel = _loginVM;
            });

            // Початковий режим
            CurrentViewModel = _loginVM;

        }
    }
}