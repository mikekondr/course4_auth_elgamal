namespace auth_elgamal.ViewModels.Dialogs
{
    // Ця VM передає дані у діалог і забирає відповідь
    public class ChallengeDialogViewModel : BaseViewModel
    {
        // ВХІДНІ ДАНІ (випадкове число x)
        private int _x_Value;
        public int X_Value
        {
            get => _x_Value;
            set { _x_Value = value; OnPropertyChanged(); }
        }

        // ВИХІДНІ ДАНІ (відповідь користувача)
        private string _answer;
        public string Answer
        {
            get => _answer;
            set { _answer = value; OnPropertyChanged(); }
        }
    }
}