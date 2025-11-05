using auth_elgamal.Models;
using auth_elgamal.Models.Keys;
using auth_elgamal.Models.Notifications;
using auth_elgamal.Services;
using MaterialDesignThemes.Wpf;
using System.Windows.Input;
using System.Windows.Media;
using auth_elgamal.Views.Dialogs;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;

namespace auth_elgamal.ViewModels
{
    public class EncryptionViewModel : BaseViewModel
    {
        // --- Сервіси та Стан ---
        private readonly ISnackbarMessageQueue _notificationQueue;
        private readonly ElGamalService _elGamalService;
        private string _currentUserLogin;

        // Сховища для завантажених ключів
        private PrivateKey _privateKey;
        private PublicKey _selectedPublicKey;

        // Імена файлів
        private const string INPUT_FILE = "input.txt";
        private const string CIPHER_FILE = "close.txt";
        private const string OUTPUT_FILE = "output.txt";

        // Кольори для статусів
        private readonly Brush _successBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
        private readonly Brush _errorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));

        // --- Властивості для прив'язки (UI) ---
        private string _plainText;
        public string PlainText
        {
            get => _plainText;
            set { _plainText = value; OnPropertyChanged(); ((RelayCommand)EncryptCommand).RaiseCanExecuteChanged(); }
        }

        private string _resultText;
        public string ResultText
        {
            get => _resultText;
            set { _resultText = value; OnPropertyChanged(); ((RelayCommand)DecryptCommand).RaiseCanExecuteChanged(); }
        }

        // Статуси ключів
        private string _privateKeyStatus;
        public string PrivateKeyStatus
        {
            get => _privateKeyStatus;
            set { _privateKeyStatus = value; OnPropertyChanged(); }
        }
        private Brush _privateKeyStatusColor;
        public Brush PrivateKeyStatusColor
        {
            get => _privateKeyStatusColor;
            set { _privateKeyStatusColor = value; OnPropertyChanged(); }
        }

        private string _publicKeyStatus;
        public string PublicKeyStatus
        {
            get => _publicKeyStatus;
            set { _publicKeyStatus = value; OnPropertyChanged(); }
        }
        private Brush _publicKeyStatusColor;
        public Brush PublicKeyStatusColor
        {
            get => _publicKeyStatusColor;
            set { _publicKeyStatusColor = value; OnPropertyChanged(); }
        }

        // Ім'я адресата (для завантаження .pub)
        private string _recipientName;
        public string RecipientName
        {
            get => _recipientName;
            set { _recipientName = value; OnPropertyChanged(); }
        }

        // --- Команди ---
        public ICommand EncryptCommand { get; }
        public ICommand DecryptCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand GenerateKeysCommand { get; }
        public ICommand LoadRecipientKeyCommand { get; }
        public ICommand LoadEncryptedFileCommand { get; }

        public EncryptionViewModel(ISnackbarMessageQueue notificationQueue)
        {
            _notificationQueue = notificationQueue;
            _elGamalService = new ElGamalService();

            PrivateKeyStatusColor = Brushes.Gray;
            PublicKeyStatusColor = Brushes.Gray;

            EncryptCommand = new RelayCommand(async (obj) => await Encrypt(obj), CanEncrypt);
            DecryptCommand = new RelayCommand(async (obj) => await Decrypt(obj), CanDecrypt);

            LoadEncryptedFileCommand = new RelayCommand(async (obj) => await LoadEncryptedFile());

            ClearCommand = new RelayCommand(ClearFields);
            GenerateKeysCommand = new RelayCommand(GenerateAndLoadKeys);

            LoadRecipientKeyCommand = new RelayCommand(async (obj) => await LoadRecipientKey(obj));
        }

        public async void Activate(User currentUser)
        {
            if (currentUser == null) return;
            _currentUserLogin = currentUser.Login;

            // Встановлюємо себе як адресата за замовчуванням
            RecipientName = _currentUserLogin;

            // Автоматично завантажуємо ключі
            await LoadUserPrivateKey();
            await LoadRecipientKey(null); // Завантажуємо ключ адресата за замовчуванням
        }

        // --- Логіка Ключів ---

        private async Task LoadUserPrivateKey()
        {
            _privateKey = await Task.Run(() => _elGamalService.LoadPrivateKey(_currentUserLogin));

            if (_privateKey != null)
            {
                PrivateKeyStatus = $"Файл {_currentUserLogin}.key завантажено.";
                PrivateKeyStatusColor = _successBrush;
            }
            else
            {
                PrivateKeyStatus = $"ПОМИЛКА: Файл {_currentUserLogin}.key не знайдено!";
                PrivateKeyStatusColor = _errorBrush;
            }
            // Оновлюємо стан кнопки "Розшифрувати"
            ((RelayCommand)DecryptCommand).RaiseCanExecuteChanged();
        }

        private async Task LoadRecipientKey(object obj)
        {
            if (string.IsNullOrEmpty(RecipientName))
            {
                PublicKeyStatus = "ПОМИЛКА: Введіть ім'я адресата (напр., 'admin' або 'user1').";
                PublicKeyStatusColor = _errorBrush;
                return;
            }

            string pubKeyFile = $"{RecipientName}.pub";
            _selectedPublicKey = await Task.Run(() => _elGamalService.LoadPublicKey(pubKeyFile));

            if (_selectedPublicKey != null)
            {
                PublicKeyStatus = $"Файл {pubKeyFile} (Адресат) завантажено.";
                PublicKeyStatusColor = _successBrush;
            }
            else
            {
                PublicKeyStatus = $"ПОМИЛКА: Файл {pubKeyFile} не знайдено!";
                PublicKeyStatusColor = _errorBrush;
            }
            // Оновлюємо стан кнопки "Зашифрувати"
            ((RelayCommand)EncryptCommand).RaiseCanExecuteChanged();
        }

        private async void GenerateAndLoadKeys(object obj)
        {
            // 3. Створюємо вигляд нашого діалогу
            var dialogView = new ConfirmDialogView();

            // 4. Викликаємо діалог і ЧЕКАЄМО на відповідь
            // Ми використовуємо Identifier, який вказали у MainWindow.xaml
            var result = await DialogHost.Show(dialogView, "RootDialogHost");

            // 5. Перевіряємо, що користувач натиснув "Так" (true)
            if (result is not string || (string)result != "true")
            {
                // Користувач натиснув "Скасувати"
                return;
            }

            // --- КОРИСТУВАЧ НАТИСНУВ "ТАК" ---
            // 1. Виконуємо БЛОКУЮЧУ роботу (File I/O) у фоновому потоці.
            bool success = await Task.Run(() =>
            {
                try
                {
                    // Цей метод виконує File.WriteAllText і має
                    // виконуватись у фоновому потоці.
                    return _elGamalService.GenerateKeys(_currentUserLogin);
                }
                catch (Exception ex)
                {
                    // Якщо у фоновому потоці сталася помилка
                    System.Diagnostics.Debug.WriteLine($"Key Gen Error: {ex.Message}");
                    return false;
                }
            });

            await Task.Delay(300);

            // Ми повернулися в UI-потік

            if (success)
            {
                LoggingService.Instance.LogEvent(_currentUserLogin, "Згенеровано нову пару ключів (після підтвердження)");

                // UI-потік ВІЛЬНИЙ. Snackbar спрацює.
                _notificationQueue.Enqueue(new SuccessNotification { Message = "Нову пару ключів згенеровано!" });

                // 'await' гарантує, що UI-потік буде вільний
                // під час читання файлів, яке відбувається всередині цих методів.
                await LoadUserPrivateKey();
                await LoadRecipientKey(null);
            }
            else
            {
                // UI-потік ВІЛЬНИЙ. Snackbar спрацює.
                _notificationQueue.Enqueue(new ErrorNotification { Message = "Помилка генерації ключів." });
            }
        }

        /// <summary>
        /// Повністю очищує всі дані сеансу (ключі, текст, статуси).
        /// Викликається при виході користувача.
        /// </summary>
        public void ClearAllData()
        {
            // Очищуємо поля вводу
            PlainText = string.Empty;
            ResultText = string.Empty;

            // Скидаємо ім'я адресата
            RecipientName = string.Empty;

            // "Забуваємо" ключі
            _privateKey = null;
            _selectedPublicKey = null;
            _currentUserLogin = null;

            // Скидаємо статуси
            PrivateKeyStatus = string.Empty;
            PrivateKeyStatusColor = Brushes.Gray;

            PublicKeyStatus = string.Empty;
            PublicKeyStatusColor = Brushes.Gray;

            // Оновлюємо (деактивуємо) кнопки
            ((RelayCommand)EncryptCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DecryptCommand).RaiseCanExecuteChanged();
        }

        // --- Логіка Шифрування ---

        private bool CanEncrypt(object obj)
        {
            // Можна шифрувати, якщо є текст І є публічний ключ адресата
            return !string.IsNullOrEmpty(PlainText) && _selectedPublicKey != null;
        }
        private async Task Encrypt(object obj)
        {
            // 1. Кешуємо дані з UI-потоку
            string textToEncrypt = PlainText;
            PublicKey key = _selectedPublicKey;
            string recipient = RecipientName;

            bool success = false;
            string cipherTextResult = null;

            try
            {
                // 2. Виконуємо всю файлову I/O роботу у фоновому потоці
                cipherTextResult = await Task.Run(() =>
                {
                    // a. Зберігаємо відкритий текст у input.txt
                    File.WriteAllText(INPUT_FILE, textToEncrypt, Encoding.UTF8);

                    // b. Шифруємо (input.txt -> close.txt)
                    _elGamalService.EncryptFile(INPUT_FILE, key, CIPHER_FILE);

                    // c. Зчитуємо результат для показу в UI
                    return File.ReadAllText(CIPHER_FILE);
                });
                success = true;
            }
            catch (Exception ex)
            {
                // Обробляємо помилки (напр., ключ недійсний)
                System.Diagnostics.Debug.WriteLine($"Encrypt Error: {ex.Message}");
                await Task.Delay(300); // Даємо час діалогу (якщо він був)
                _notificationQueue.Enqueue(new ErrorNotification { Message = $"Помилка шифрування: {ex.Message}" });
            }

            // 3. Повертаємося в UI-потік та оновлюємо UI
            if (success)
            {
                ResultText = cipherTextResult; // Показуємо шифротекст
                PlainText = string.Empty; // Очищуємо поле
                LoggingService.Instance.LogEvent(_currentUserLogin, $"Виконано шифрування для {recipient} (файл {CIPHER_FILE})");
                _notificationQueue.Enqueue(new SuccessNotification { Message = "Повідомлення зашифровано та збережено у close.txt" });
            }
        }

        private bool CanDecrypt(object obj)
        {
            // Можна розшифрувати, якщо є текст І є наш приватний ключ
            return !string.IsNullOrEmpty(ResultText) && _privateKey != null;
        }
        private async Task Decrypt(object obj)
        {
            // 1. Кешуємо дані з UI-потоку
            string textToDecrypt = ResultText;
            PrivateKey key = _privateKey;

            bool success = false;
            string plainTextResult = null;

            try
            {
                // 2. Виконуємо всю файлову I/O роботу у фоновому потоці
                plainTextResult = await Task.Run(() =>
                {
                    // a. Зберігаємо шифротекст (з UI) у close.txt
                    File.WriteAllText(CIPHER_FILE, textToDecrypt);

                    // b. Розшифровуємо (close.txt -> output.txt)
                    _elGamalService.DecryptFile(CIPHER_FILE, key, OUTPUT_FILE);

                    // c. Зчитуємо результат для показу в UI
                    return File.ReadAllText(OUTPUT_FILE, Encoding.UTF8);
                });
                success = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Decrypt Error: {ex.Message}");
                await Task.Delay(300);
                _notificationQueue.Enqueue(new ErrorNotification { Message = $"Помилка розшифрування: {ex.Message}" });
            }

            // 3. Повертаємося в UI-потік та оновлюємо UI
            if (success)
            {
                PlainText = plainTextResult; // Показуємо розшифрований текст
                ResultText = string.Empty; // Очищуємо поле
                LoggingService.Instance.LogEvent(_currentUserLogin, $"Виконано розшифрування (файл {OUTPUT_FILE})");
                _notificationQueue.Enqueue(new SuccessNotification { Message = "Повідомлення розшифровано та збережено у output.txt" });
            }
        }

        private async Task LoadEncryptedFile()
        {
            if (!File.Exists(CIPHER_FILE))
            {
                _notificationQueue.Enqueue(new ErrorNotification { Message = $"Файл {CIPHER_FILE} не знайдено." });
                return;
            }

            try
            {
                // Асинхронно читаємо файл у фоновому потоці
                string content = await Task.Run(() => File.ReadAllText(CIPHER_FILE));

                // Оновлюємо UI (ми повернулися в UI-потік)
                ResultText = content;

                LoggingService.Instance.LogEvent(_currentUserLogin, $"Завантажено вміст файлу {CIPHER_FILE}");
                _notificationQueue.Enqueue(new SuccessNotification { Message = $"Вміст файлу {CIPHER_FILE} завантажено." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load Encrypted Error: {ex.Message}");
                _notificationQueue.Enqueue(new ErrorNotification { Message = $"Не вдалося прочитати файл: {ex.Message}" });
            }
        }

        private void ClearFields(object obj)
        {
            PlainText = string.Empty;
            ResultText = string.Empty;
        }
    }
}