using auth_elgamal.Models;
using auth_elgamal.Services;
using auth_elgamal.ViewModels.Dialogs;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;

namespace auth_elgamal.ViewModels
{
    /// <summary>
    /// Головне вікно програми, яке керує навігацією між різними режимами (Login, Admin, User).
    /// Тут же реалізовано логіку періодичної перевірки користувача у User режимі.
    /// Підтримує Snackbar для повідомлень та індикатор завантаження.
    /// </summary>
    public class MainWindowViewModel : BaseViewModel
    {
        // Властивості для навігації між режимами
        private BaseViewModel _currentViewModel;
        private User _currentUser;

        private readonly LoginViewModel _loginVM;
        private readonly AdminViewModel _adminVM;
        private readonly UserViewModel _userVM;

        // Періодична перевірка користувача
        // Відповідь - результат формули A * x^b
        private CancellationTokenSource _challengeCts;
        private readonly Random _random = new Random();
        private const double A_VALUE = 4.0;
        private const double EXPONENT = 0.85;

        // Флаг "заянятості" для індикатора завантаження
        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged();
            }
        }

        // Делегат для показу/приховування індикатора завантаження
        private readonly Action<bool> _showLoading;

        // Черга повідомлень для Snackbar
        public ISnackbarMessageQueue NotificationQueue { get; }

        // Властивість, до якої прив'язаний ContentControl
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

        // Команди навігації
        public ICommand GoToLoginCommand { get; }
        public ICommand GoToAdminCommand { get; }
        public ICommand GoToUserCommand { get; }

        public MainWindowViewModel()
        {
            // Ініціалізація черги повідомлень Snackbar
            NotificationQueue = new SnackbarMessageQueue(TimeSpan.FromSeconds(3));

            // Делегат для показу/приховування індикатора завантаження
            _showLoading = (isLoading) => IsBusy = isLoading;

            // екземпляри ViewModel для кожного режиму
            _loginVM = new LoginViewModel(this);
            _adminVM = new AdminViewModel(this, NotificationQueue);
            _userVM = new UserViewModel(this, NotificationQueue, _showLoading);

            // При переході на AdminView...
            GoToAdminCommand = new RelayCommand(_ =>
            {
                CurrentViewModel = _adminVM;
                // Зупиняємо періодичну перевірку користувача, коли входить адмін
                StopChallengeLoop();
            });

            // При переході на UserView...
            GoToUserCommand = new RelayCommand(_ =>
            {
                CurrentViewModel = _userVM;
                // "активуємо" UserViewModel, щоб оновились дані
                _userVM.Activate();

                // Запускаємо цикл періодичної перевірки користувача
                StartChallengeLoop();
            });

            // При виході (GoToLogin) - очищуємо сесію
            GoToLoginCommand = new RelayCommand(_ =>
            {
                // Зупиняємо перевірку при виході
                StopChallengeLoop();

                LoggingService.Instance.LogEvent(CurrentUser.Login, "Вихід із системи");

                CurrentUser = null;
                _userVM.ClearSessionData();

                CurrentViewModel = _loginVM;
            });

            // Початковий режим
            CurrentViewModel = _loginVM;
        }

        // Запускає цикл періодичної перевірки користувача
        private void StartChallengeLoop()
        {
            // Зупиняємо попередній цикл, якщо він був
            StopChallengeLoop();

            _challengeCts = new CancellationTokenSource();

            // Запускаємо асинхронний цикл у фоні, не блокуючи UI
            _ = RunChallengeLoop(_challengeCts.Token);
        }

        // Зупиняє цикл періодичної перевірки користувача
        private void StopChallengeLoop()
        {
            if (_challengeCts != null)
            {
                _challengeCts.Cancel();
                _challengeCts.Dispose();
                _challengeCts = null;
            }
        }

        // Асинхронний цикл періодичної перевірки користувача
        private async Task RunChallengeLoop(CancellationToken token)
        {
            try
            {
                // Допоки не встановлено флаг скасування...
                while (!token.IsCancellationRequested)
                {
                    // Чекаємо випадковий час T (від 30 до 60 секунд)
                    int waitSeconds = _random.Next(30, 61);
                    await Task.Delay(waitSeconds * 1000, token);

                    if (token.IsCancellationRequested) return;

                    // Генеруємо питання
                    int x = _random.Next(100, 1000); // Випадкове число x
                    double correctAnswer = A_VALUE * Math.Pow(x, EXPONENT); // Правильна відповідь

                    // Діалог перевірки
                    var challengeVM = new ChallengeDialogViewModel { X_Value = x };

                    // Показ діалогу
                    var result = await DialogHost.Show(challengeVM, "RootDialogHost");

                    if (token.IsCancellationRequested) return;

                    // Перевіряємо відповідь
                    if (result is ChallengeDialogViewModel vm &&
                        double.TryParse(vm.Answer?.Replace('.', ','), out double userAnswer))
                    {
                        // Порівнюємо з точністю до 2 знаків після коми
                        if (Math.Abs(userAnswer - correctAnswer) < 0.01)
                        {
                            // ПРАВИЛЬНО
                            LoggingService.Instance.LogEvent(CurrentUser.Login, $"Успішно пройшов перевірку (x={x})");
                            NotificationQueue.Enqueue(new Models.Notifications.SuccessNotification { Message = "Перевірку пройдено." });
                        }
                        else
                        {
                            // НЕПРАВИЛЬНО
                            LoggingService.Instance.LogEvent(CurrentUser.Login, $"Помилка перевірки (x={x}, введено={userAnswer}, очікувалось={correctAnswer:F2})");
                            await Task.Delay(300); // Даємо час для зникнення діалогу, щоб Snackbar з'явився
                            NotificationQueue.Enqueue(new Models.Notifications.ErrorNotification { Message = "Помилка перевірки. Доступ заборонено." });
                            GoToLoginCommand.Execute(null); // Викидаємо користувача на сторінку входу
                        }
                    }
                    else
                    {
                        // НЕПРАВИЛЬНО (введено не число або закрито діалог)
                        LoggingService.Instance.LogEvent(CurrentUser.Login, $"Помилка перевірки (невірний формат відповіді)");
                        await Task.Delay(300);
                        NotificationQueue.Enqueue(new Models.Notifications.ErrorNotification { Message = "Невірний формат відповіді. Доступ заборонено." });
                        GoToLoginCommand.Execute(null);
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Це нормально, цикл зупинився (напр., через вихід з програми)
            }
        }
    }
}