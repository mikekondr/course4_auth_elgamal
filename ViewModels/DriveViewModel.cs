using auth_elgamal.Models;
using auth_elgamal.ViewModels.SubViewModels;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;

namespace auth_elgamal.ViewModels
{
    public class DriveViewModel : BaseViewModel
    {
        private readonly ISnackbarMessageQueue _notificationQueue;
        private string _currentUserLogin;

        public ObservableCollection<DiskViewModel> Disks { get; }

        public DriveViewModel(ISnackbarMessageQueue notificationQueue)
        {
            _notificationQueue = notificationQueue;
            Disks = new ObservableCollection<DiskViewModel>();
        }

        public void Activate(User currentUser)
        {
            Disks.Clear();

            if (currentUser == null || currentUser.IsAdmin) return;

            _currentUserLogin = currentUser.Login;

            foreach (var permission in currentUser.Permissions)
            {
                string diskLetter = permission.Key;
                string rights = permission.Value;

                // Створюємо ViewModel для диска (картки)
                Disks.Add(new DiskViewModel(diskLetter, rights, _notificationQueue, _currentUserLogin));
            }
        }

        public void ClearData()
        {
            Disks.Clear();
            _currentUserLogin = null;
        }
    }
}