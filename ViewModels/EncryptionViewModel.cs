using auth_elgamal.Models;
using auth_elgamal.Models.Keys;
using auth_elgamal.Models.Notifications;
using auth_elgamal.Services;
using auth_elgamal.Views.Dialogs;
using MaterialDesignThemes.Wpf;
using System.IO;
using System.Text;
using System.Windows.Input;
using System.Windows.Media;

namespace auth_elgamal.ViewModels
{
    public class EncryptionViewModel : BaseViewModel
    {
        // Сервіси та Стан
        private readonly ISnackbarMessageQueue _notificationQueue;
        private readonly ElGamalService _elGamalService;
        private string _currentUserLogin;

        // Показ/приховування індикатора завантаження
        private readonly Action<bool> _showLoading;

        // Сховища для завантажених ключів
        private PrivateKey _privateKey;
        private PublicKey _selectedPublicKey;

        // Імена файлів повідомлень
        private const string INPUT_FILE = "input.txt";
        private const string CIPHER_FILE = "close.txt";
        private const string OUTPUT_FILE = "out.txt";

        // Властивості для елементів керування
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

        // Кольори для статусів ключів
        private readonly Brush _successBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2E7D32"));
        private readonly Brush _errorBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#C62828"));

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

        // Ім'я адресата (для завантаження відкритого ключа .pub)
        private string _recipientName;
        public string RecipientName
        {
            get => _recipientName;
            set { _recipientName = value; OnPropertyChanged(); }
        }

        // Команди
        public ICommand EncryptCommand { get; }
        public ICommand DecryptCommand { get; }
        public ICommand ClearCommand { get; }
        public ICommand GenerateKeysCommand { get; }
        public ICommand LoadRecipientKeyCommand { get; }
        public ICommand LoadEncryptedFileCommand { get; }

        public EncryptionViewModel(ISnackbarMessageQueue notificationQueue, Action<bool> showLoading)
        {
            _notificationQueue = notificationQueue;
            _elGamalService = new ElGamalService();
            _showLoading = showLoading;

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

            // За замовчуванням ім'я адресата - це поточний користувач
            RecipientName = _currentUserLogin;

            // Автоматично завантажуємо ключі поточного користувача
            await LoadUserPrivateKey();
            await LoadRecipientKey(null);
        }

        private async Task LoadUserPrivateKey()
        {
            _showLoading(true);
            try
            {
                _privateKey = await Task.Run(() => _elGamalService.LoadPrivateKey(_currentUserLogin));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            finally { _showLoading(false); }

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

            _showLoading(true);
            try
            {
                _selectedPublicKey = await Task.Run(() => _elGamalService.LoadPublicKey(pubKeyFile));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            finally { _showLoading(false); }

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
            ((RelayCommand)EncryptCommand).RaiseCanExecuteChanged();
        }

        private async void GenerateAndLoadKeys(object obj)
        {
            // Діалог підтвердження генерації ключів
            var dialogView = new ConfirmDialogView();
            var result = await DialogHost.Show(dialogView, "RootDialogHost");

            if (result is not string || (string)result != "true") { return; }

            bool success = false;

            _showLoading(true);
            try
            {
                success = await Task.Run(() =>
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
                // пауза для інтерфейсу
                await Task.Delay(300);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex); }
            finally { _showLoading(false); }

            if (success)
            {
                LoggingService.Instance.LogEvent(_currentUserLogin, "Згенеровано нову пару ключів (після підтвердження)");
                _notificationQueue.Enqueue(new SuccessNotification { Message = "Нову пару ключів згенеровано!" });
                // і одразу завантажуємо згенеровану пару
                await LoadUserPrivateKey();
                await LoadRecipientKey(null);
            }
            else { _notificationQueue.Enqueue(new ErrorNotification { Message = "Помилка генерації ключів." }); }
        }

        public void ClearAllData()
        {
            PlainText = string.Empty;
            ResultText = string.Empty;

            RecipientName = string.Empty;

            _privateKey = null;
            _selectedPublicKey = null;
            _currentUserLogin = null;

            PrivateKeyStatus = string.Empty;
            PrivateKeyStatusColor = Brushes.Gray;

            PublicKeyStatus = string.Empty;
            PublicKeyStatusColor = Brushes.Gray;

            ((RelayCommand)EncryptCommand).RaiseCanExecuteChanged();
            ((RelayCommand)DecryptCommand).RaiseCanExecuteChanged();
        }

        private bool CanEncrypt(object obj)
        {
            // Можна шифрувати, якщо є текст І є публічний ключ адресата
            return !string.IsNullOrEmpty(PlainText) && _selectedPublicKey != null;
        }
        private async Task Encrypt(object obj)
        {
            string textToEncrypt = PlainText;
            PublicKey key = _selectedPublicKey;
            string recipient = RecipientName;

            bool success = false;
            string cipherTextResult = null;

            _showLoading(true);
            try
            {
                cipherTextResult = await Task.Run(() =>
                    {
                        File.WriteAllText(INPUT_FILE, textToEncrypt, Encoding.UTF8);
                        _elGamalService.EncryptFile(INPUT_FILE, key, CIPHER_FILE);
                        return File.ReadAllText(CIPHER_FILE);
                    });
                success = true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Encrypt Error: {ex.Message}");
                await Task.Delay(300);
                _notificationQueue.Enqueue(new ErrorNotification { Message = $"Помилка шифрування: {ex.Message}" });
            }
            finally { _showLoading(false); }

            if (success)
            {
                ResultText = cipherTextResult;
                PlainText = string.Empty;
                LoggingService.Instance.LogEvent(_currentUserLogin, $"Виконано шифрування для {recipient} (файл {CIPHER_FILE})");
                _notificationQueue.Enqueue(new SuccessNotification { Message = $"Повідомлення зашифровано та збережено у {CIPHER_FILE}" });
            }
        }

        private bool CanDecrypt(object obj)
        {
            // Можна розшифрувати, якщо є текст І є наш приватний ключ
            return !string.IsNullOrEmpty(ResultText) && _privateKey != null;
        }
        private async Task Decrypt(object obj)
        {
            string textToDecrypt = ResultText;
            PrivateKey key = _privateKey;

            bool success = false;
            string plainTextResult = null;

            _showLoading(true);
            try
            {
                plainTextResult = await Task.Run(() =>
                    {
                        File.WriteAllText(CIPHER_FILE, textToDecrypt);
                        _elGamalService.DecryptFile(CIPHER_FILE, key, OUTPUT_FILE);
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
            finally { _showLoading(false); }

            if (success)
            {
                PlainText = plainTextResult;
                ResultText = string.Empty;
                LoggingService.Instance.LogEvent(_currentUserLogin, $"Виконано розшифрування (файл {OUTPUT_FILE})");
                _notificationQueue.Enqueue(new SuccessNotification { Message = $"Повідомлення розшифровано та збережено у {OUTPUT_FILE}" });
            }
        }

        private async Task LoadEncryptedFile()
        {
            if (!File.Exists(CIPHER_FILE))
            {
                _notificationQueue.Enqueue(new ErrorNotification { Message = $"Файл {CIPHER_FILE} не знайдено." });
                return;
            }

            _showLoading(true);
            try
            {
                string content = await Task.Run(() => File.ReadAllText(CIPHER_FILE));
                ResultText = content;

                LoggingService.Instance.LogEvent(_currentUserLogin, $"Завантажено вміст файлу {CIPHER_FILE}");
                _notificationQueue.Enqueue(new SuccessNotification { Message = $"Вміст файлу {CIPHER_FILE} завантажено." });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Load Encrypted Error: {ex.Message}");
                _notificationQueue.Enqueue(new ErrorNotification { Message = $"Не вдалося прочитати файл: {ex.Message}" });
            }
            finally { _showLoading(false); }
        }

        private void ClearFields(object obj)
        {
            PlainText = string.Empty;
            ResultText = string.Empty;
        }
    }
}