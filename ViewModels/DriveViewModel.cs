using auth_elgamal.Models; // Потрібно для User
using auth_elgamal.ViewModels.SubViewModels; // Потрібно для DiskViewModel
using MaterialDesignThemes.Wpf; // Потрібно для Snackbar
using System.Collections.ObjectModel;

namespace auth_elgamal.ViewModels
{
    public class DriveViewModel : BaseViewModel
    {
        private readonly ISnackbarMessageQueue _notificationQueue;
        private string _currentUserLogin;

        // Сюди будуть прив'язані картки
        public ObservableCollection<DiskViewModel> Disks { get; }

        // Ми передамо чергу сповіщень з UserViewModel
        public DriveViewModel(ISnackbarMessageQueue notificationQueue)
        {
            _notificationQueue = notificationQueue;
            Disks = new ObservableCollection<DiskViewModel>();
        }

        /// <summary>
        /// Цей метод "активує" ViewModel, заповнюючи її даними 
        /// поточного користувача.
        /// </summary>
        public void Activate(User currentUser)
        {
            // Очищуємо диски від попереднього користувача
            Disks.Clear();

            if (currentUser == null || currentUser.IsAdmin) return;

            _currentUserLogin = currentUser.Login;

            // Проходимо по словнику прав користувача (напр., Key="A", Value="RWE")
            foreach (var permission in currentUser.Permissions)
            {
                string diskLetter = permission.Key;
                string rights = permission.Value;

                // Створюємо ViewModel для одного диска (картки)
                // і передаємо йому чергу сповіщень
                Disks.Add(new DiskViewModel(diskLetter, rights, _notificationQueue, _currentUserLogin));
            }
        }
    }
}