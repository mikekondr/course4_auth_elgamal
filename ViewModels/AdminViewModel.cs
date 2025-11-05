using auth_elgamal.Models.Notifications;
using auth_elgamal.Services;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;

namespace auth_elgamal.ViewModels
{
    public class AdminViewModel : BaseViewModel
    {
        private readonly MainWindowViewModel _mainVM;
        private readonly UserService _userService;
        private readonly ISnackbarMessageQueue _notificationQueue;

        // --- Колекції та Виділення ---
        public ObservableCollection<UserEntryViewModel> Users { get; }

        private UserEntryViewModel _selectedUser;
        public UserEntryViewModel SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();

                // Коли ми обираємо користувача, ми копіюємо його
                // дані у 'EditingUser' для безпечного редагування
                CopySelectedToEditing();

                // Оновлюємо стан кнопки "Видалити"
                DeleteCommand.RaiseCanExecuteChanged();
            }
        }

        private UserEntryViewModel _editingUser;
        // Цей об'єкт прив'язаний до полів редагування
        public UserEntryViewModel EditingUser
        {
            get => _editingUser;
            set { _editingUser = value; OnPropertyChanged(); }
        }

        private string _editingPanelHeader = "Оберіть користувача або додайте нового";
        public string EditingPanelHeader
        {
            get => _editingPanelHeader;
            set { _editingPanelHeader = value; OnPropertyChanged(); }
        }

        // --- Команди ---
        public RelayCommand LogoutCommand { get; }
        public RelayCommand AddNewCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand SaveCommand { get; }

        private bool _isEditingExistingUser = false;
        private const int MAX_USERS = 14;

        public AdminViewModel(MainWindowViewModel mainVM, ISnackbarMessageQueue notificationQueue)
        {
            _mainVM = mainVM;
            _userService = new UserService();
            _notificationQueue = notificationQueue;
            Users = new ObservableCollection<UserEntryViewModel>();

            // Ініціалізація команд
            LogoutCommand = new RelayCommand(_ => _mainVM.GoToLoginCommand.Execute(null));
            AddNewCommand = new RelayCommand(AddNew, CanAddNew);
            DeleteCommand = new RelayCommand(Delete, CanDelete);
            SaveCommand = new RelayCommand(Save, CanSave);

            // Завантажуємо користувачів при першому відкритті
            LoadUsersList();
        }

        private void LoadUsersList()
        {
            Users.Clear();
            var loadedUsers = _userService.LoadUsers();
            foreach (var user in loadedUsers)
            {
                Users.Add(user);
            }
            // Оновлюємо стан кнопки "Додати"
            AddNewCommand.RaiseCanExecuteChanged();
        }

        // --- Логіка Команд ---

        private void CopySelectedToEditing()
        {
            if (SelectedUser == null)
            {
                // Якщо виділення знято, очищуємо панель
                EditingUser = null;
                EditingPanelHeader = "Оберіть користувача або додайте нового";
                _isEditingExistingUser = false;
            }
            else
            {
                // Створюємо ГЛИБОКУ КОПІЮ для редагування
                EditingUser = new UserEntryViewModel(
                    SelectedUser.Login,
                    SelectedUser.Password,
                    SelectedUser.GetPermissionString()
                );
                EditingPanelHeader = $"Редагування: {EditingUser.Login}";
                _isEditingExistingUser = true;
            }
            SaveCommand.RaiseCanExecuteChanged();
        }

        // --- Додати ---
        private bool CanAddNew(object obj)
        {
            // Не дозволяємо додавати, якщо досягнуто ліміту
            return Users.Count < MAX_USERS;
        }
        private void AddNew(object obj)
        {
            SelectedUser = null; // Знімаємо виділення з DataGrid
            EditingUser = new UserEntryViewModel(); // Створюємо порожній об'єкт
            EditingPanelHeader = "Додавання нового користувача";
            _isEditingExistingUser = false;
            SaveCommand.RaiseCanExecuteChanged();
        }

        // --- Видалити ---
        private bool CanDelete(object obj)
        {
            // Можна видалити, тільки якщо хтось виділений
            return SelectedUser != null;
        }
        private void Delete(object obj)
        {
            if (SelectedUser == null) return;

            // Видаляємо з колекції (UI оновиться)
            string strLogin = SelectedUser.Login;
            Users.Remove(SelectedUser);

            // Зберігаємо зміни у файл
            if (_userService.SaveUsers(Users))
            {
                _notificationQueue.Enqueue(new SuccessNotification { Message = $"Користувача {strLogin} видалено." });
            }
            else
            {
                _notificationQueue.Enqueue(new ErrorNotification { Message = "Помилка: не вдалося зберегти зміни." });
                LoadUsersList(); // Відновлюємо список з файлу
            }

            SelectedUser = null; // Очищуємо виділення
            AddNewCommand.RaiseCanExecuteChanged(); // Перевіряємо ліміт
        }

        // --- Зберегти ---
        private bool CanSave(object obj)
        {
            // Можна зберегти, тільки якщо панель редагування активна
            return EditingUser != null;
        }
        private void Save(object obj)
        {
            // 1. Валідація
            if (string.IsNullOrWhiteSpace(EditingUser.Login) ||
                string.IsNullOrWhiteSpace(EditingUser.Password))
            {
                _notificationQueue.Enqueue(new ErrorNotification { Message = "Помилка: Логін та Пароль не можуть бути порожніми." });
                return;
            }

            // Зберігаємо логін, оскільки EditingUser скоро може стати null
            string savedLogin = EditingUser.Login;

            // 2. Логіка збереження
            if (_isEditingExistingUser)
            {
                // --- РЕДАГУВАННЯ ---
                var originalUser = Users.FirstOrDefault(u => u.Login == EditingUser.Login);
                if (originalUser != null)
                {
                    // Оновлюємо властивості існуючого об'єкта
                    originalUser.UpdateFrom(EditingUser);
                }
            }
            else
            {
                // --- ДОДАВАННЯ ---
                // Перевірка на дублікат логіну
                if (Users.Any(u => u.Login.Equals(EditingUser.Login,
                                 System.StringComparison.OrdinalIgnoreCase)))
                {
                    _notificationQueue.Enqueue(new ErrorNotification { Message = "Помилка: Користувач з таким логіном вже існує." });
                    return;
                }

                // Перевірка ліміту
                if (!CanAddNew(null))
                {
                    _notificationQueue.Enqueue(new ErrorNotification { Message = "Помилка: Досягнуто ліміту користувачів (14)." });
                    return;
                }

                EditingUser.LoginIsEditable = false;
                Users.Add(EditingUser);
                AddNewCommand.RaiseCanExecuteChanged(); // Оновлюємо стан кнопки "Додати"
            }

            // 3. Запис у файл
            if (_userService.SaveUsers(Users))
            {
                // Встановлюємо повідомлення про успіх
                _notificationQueue.Enqueue(new SuccessNotification { Message = $"Дані {savedLogin} збережено." });
            }
            else
            {
                _notificationQueue.Enqueue(new ErrorNotification { Message = "Помилка: не вдалося зберегти зміни у файл." });
                LoadUsersList();
                return;
            }

            // 4. --- РЕАЛІЗАЦІЯ ВАШОГО ЗАПИТУ ---
            // Скидаємо виділення рядка в таблиці.
            // Це автоматично викличе setter 'SelectedUser', 
            // який викличе 'CopySelectedToEditing', 
            // який, побачивши 'SelectedUser == null', встановить 'EditingUser = null'.
            // А 'EditingUser = null' автоматично очистить і заблокує форму.
            SelectedUser = null;
        }
    }
}