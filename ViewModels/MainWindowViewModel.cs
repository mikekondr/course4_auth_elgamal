using auth_elgamal.Models;
using auth_elgamal.Services;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;
using System.Threading; // Для CancellationTokenSource
using System.Threading.Tasks; // Для Task
using auth_elgamal.ViewModels.Dialogs; // Для нашого діалогу

namespace auth_elgamal.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        private BaseViewModel _currentViewModel;
        private User _currentUser;

        private readonly LoginViewModel _loginVM;
        private readonly AdminViewModel _adminVM;
        private readonly UserViewModel _userVM;

        // --- Логіка Перевірки Користувача ---
        private CancellationTokenSource _challengeCts;
        private readonly Random _random = new Random();
        private const double A_VALUE = 4.0;
        private const double EXPONENT = 0.85;
        // ------------------------------------

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
            GoToAdminCommand = new RelayCommand(_ => {
                // Зупиняємо перевірку, коли входить адмін
                StopChallengeLoop();

                CurrentViewModel = _adminVM;
            });

            // При переході на UserView...
            GoToUserCommand = new RelayCommand(_ =>
            {
                // ...ми "активуємо" UserVM, щоб вона оновила дані
                _userVM.Activate();
                CurrentViewModel = _userVM;

                // Запускаємо цикл перевірки для користувача
                StartChallengeLoop();
            });

            // При виході (GoToLogin) - очищуємо сесію
            GoToLoginCommand = new RelayCommand(_ =>
            {
                // Зупиняємо перевірку при виході
                StopChallengeLoop();

                LoggingService.Instance.LogEvent(CurrentUser.Login, "Вихід із системи");

                CurrentUser = null; // Очищуємо поточного користувача

                // Викликаємо очищення для всіх під-систем UserView
                _userVM.ClearSessionData();

                // (Опціонально: можна додати _adminVM.ClearSessionData(), якщо потрібно)

                CurrentViewModel = _loginVM; // Переходимо на логін
            });

            // Початковий режим
            CurrentViewModel = _loginVM;
        }

        // --- Методи циклу перевірки ---

        private void StartChallengeLoop()
        {
            // Зупиняємо попередній цикл, якщо він був
            StopChallengeLoop();

            _challengeCts = new CancellationTokenSource();

            // Запускаємо цикл у фоні, не блокуючи UI
            _ = RunChallengeLoop(_challengeCts.Token);
        }

        private void StopChallengeLoop()
        {
            if (_challengeCts != null)
            {
                _challengeCts.Cancel(); // Відправляємо сигнал скасування
                _challengeCts.Dispose();
                _challengeCts = null;
            }
        }

        private async Task RunChallengeLoop(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    // 1. Чекаємо випадковий час T
                    // (Наприклад, від 30 до 60 секунд для тестування)
                    // (Для релізу можна поставити 5-10 хвилин)
                    int waitSeconds = _random.Next(30, 61);
                    await Task.Delay(waitSeconds * 1000, token);

                    if (token.IsCancellationRequested) return;

                    // 2. Генеруємо питання
                    int x = _random.Next(1000, 10000); // Випадкове число x
                    double correctAnswer = A_VALUE * Math.Pow(x, EXPONENT);

                    var challengeVM = new ChallengeDialogViewModel { X_Value = x };

                    // 3. Показуємо діалог (у UI-потоці)
                    // Важливо: ми не можемо вийти з діалогу, не відповівши
                    var result = await DialogHost.Show(challengeVM, "RootDialogHost");

                    if (token.IsCancellationRequested) return;

                    // 4. Перевіряємо відповідь
                    if (result is ChallengeDialogViewModel vm &&
                        double.TryParse(vm.Answer?.Replace('.', ','), out double userAnswer))
                    {
                        // Порівнюємо з точністю до 2 знаків після коми
                        if (Math.Abs(userAnswer - correctAnswer) < 0.01)
                        {
                            // ПРАВИЛЬНО
                            LoggingService.Instance.LogEvent(CurrentUser.Login, $"Успішно пройшов перевірку (x={x})");
                            // (Можна додати сповіщення про успіх)
                            NotificationQueue.Enqueue(new Models.Notifications.SuccessNotification { Message = "Перевірку пройдено." });
                        }
                        else
                        {
                            // НЕПРАВИЛЬНО
                            LoggingService.Instance.LogEvent(CurrentUser.Login, $"Помилка перевірки (x={x}, введено={userAnswer}, очікувалось={correctAnswer:F2})");
                            await Task.Delay(300); // Даємо час Snackbar з'явитися
                            NotificationQueue.Enqueue(new Models.Notifications.ErrorNotification { Message = "Помилка перевірки. Доступ заборонено." });
                            GoToLoginCommand.Execute(null); // Викидаємо користувача
                        }
                    }
                    else
                    {
                        // НЕПРАВИЛЬНО (введено не число або закрито діалог)
                        LoggingService.Instance.LogEvent(CurrentUser.Login, $"Помилка перевірки (невірний формат відповіді)");
                        await Task.Delay(300);
                        NotificationQueue.Enqueue(new Models.Notifications.ErrorNotification { Message = "Невірний формат відповіді. Доступ заборонено." });
                        GoToLoginCommand.Execute(null); // Викидаємо користувача
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // Це нормально, цикл зупинився (напр., через вихід)
            }
        }
    }
}