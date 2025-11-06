using auth_elgamal.Models.Notifications;
using auth_elgamal.Services;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;

namespace auth_elgamal.ViewModels
{
    public class AdminViewModel : BaseViewModel
    {
        private const int MAX_USERS = 14;

        private readonly MainWindowViewModel _mainVM;
        private readonly UserService _userService;
        private readonly ISnackbarMessageQueue _notificationQueue;

        public ObservableCollection<UserEntryViewModel> Users { get; }

        private bool _isEditingExistingUser = false;

        private UserEntryViewModel _selectedUser;
        public UserEntryViewModel SelectedUser
        {
            get => _selectedUser;
            set
            {
                _selectedUser = value;
                OnPropertyChanged();

                // Коли ми обираємо користувача у списку, ми копіюємо його
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
            set
            {
                _editingUser = value;
                OnPropertyChanged();
            }
        }

        private string _editingPanelHeader = "Оберіть користувача або додайте нового";
        public string EditingPanelHeader
        {
            get => _editingPanelHeader;
            set
            {
                _editingPanelHeader = value;
                OnPropertyChanged();
            }
        }

        public RelayCommand LogoutCommand { get; }
        public RelayCommand AddNewCommand { get; }
        public RelayCommand DeleteCommand { get; }
        public RelayCommand SaveCommand { get; }

        public AdminViewModel(MainWindowViewModel mainVM, ISnackbarMessageQueue notificationQueue)
        {
            _mainVM = mainVM;
            _userService = new UserService();
            _notificationQueue = notificationQueue;
            Users = new ObservableCollection<UserEntryViewModel>();

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

            AddNewCommand.RaiseCanExecuteChanged();
        }

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
                // Створюємо копію об'єкта користувача для редагування
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

        private bool CanDelete(object obj)
        {
            // Можна видалити, тільки якщо хтось виділений
            return SelectedUser != null;
        }
        private void Delete(object obj)
        {
            if (SelectedUser == null) return;

            string strLogin = SelectedUser.Login;
            Users.Remove(SelectedUser);

            // Зберігаємо зміни у файл
            if (_userService.SaveUsers(Users))
            {
                LoggingService.Instance.LogEvent(_mainVM.CurrentUser.Login, $"Видалено користувача: {strLogin}");
                _notificationQueue.Enqueue(new SuccessNotification { Message = $"Користувача {strLogin} видалено." });
            }
            else
            {
                _notificationQueue.Enqueue(new ErrorNotification { Message = "Помилка: не вдалося зберегти зміни." });
                LoadUsersList(); // Оновлюємо список з файлу
            }

            SelectedUser = null; // Очищуємо виділення
            AddNewCommand.RaiseCanExecuteChanged();
        }

        private bool CanSave(object obj)
        {
            // Можна зберегти, тільки якщо панель редагування активна
            return EditingUser != null;
        }

        private void Save(object obj)
        {
            if (string.IsNullOrWhiteSpace(EditingUser.Login) ||
                string.IsNullOrWhiteSpace(EditingUser.Password))
            {
                _notificationQueue.Enqueue(new ErrorNotification { Message = "Помилка: Логін та Пароль не можуть бути порожніми." });
                return;
            }

            string savedLogin = EditingUser.Login;

            if (_isEditingExistingUser)
            {
                // --- РЕДАГУВАННЯ ---
                var originalUser = Users.FirstOrDefault(u => u.Login == EditingUser.Login);
                if (originalUser != null)
                {
                    // Оновлюємо властивості існуючого об'єкта
                    originalUser.UpdateFrom(EditingUser);
                    LoggingService.Instance.LogEvent(_mainVM.CurrentUser.Login, $"Змінено дані користувача: {savedLogin}");
                }
            }
            else
            {
                // --- ДОДАВАННЯ ---
                if (Users.Any(u => u.Login.Equals(EditingUser.Login, System.StringComparison.OrdinalIgnoreCase)))
                {
                    _notificationQueue.Enqueue(new ErrorNotification { Message = "Помилка: Користувач з таким логіном вже існує." });
                    return;
                }

                if (!CanAddNew(null))
                {
                    _notificationQueue.Enqueue(new ErrorNotification { Message = "Помилка: Досягнуто ліміту користувачів (14)." });
                    return;
                }

                EditingUser.LoginIsEditable = false;
                Users.Add(EditingUser);
                LoggingService.Instance.LogEvent(_mainVM.CurrentUser.Login, $"Створено нового користувача: {savedLogin}");
                AddNewCommand.RaiseCanExecuteChanged();
            }

            if (_userService.SaveUsers(Users))
            {
                _notificationQueue.Enqueue(new SuccessNotification { Message = $"Дані {savedLogin} збережено." });
            }
            else
            {
                _notificationQueue.Enqueue(new ErrorNotification { Message = "Помилка: не вдалося зберегти зміни у файл." });
                LoadUsersList();
                return;
            }

            SelectedUser = null;
        }
    }
}