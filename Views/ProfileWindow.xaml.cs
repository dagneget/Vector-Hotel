using System.Windows;
using HRS.ViewModels;

namespace HRS.Views
{
    public partial class ProfileWindow : Window
    {
        public ProfileWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var viewModel = DataContext as ProfileViewModel;
            if (viewModel != null)
            {
                // Assign password fields from view elements to viewmodel safely
                viewModel.CurrentPassword = CurrentPasswordBox.Password;
                viewModel.NewPassword = NewPasswordBox.Password;
                viewModel.ConfirmPassword = ConfirmPasswordBox.Password;

                // Execute viewmodel's update command
                viewModel.UpdatePasswordCommand.Execute(null);

                // If success, we can optionally clear fields or close the window
                if (string.IsNullOrWhiteSpace(viewModel.NewPassword)) 
                {
                    CurrentPasswordBox.Clear();
                    NewPasswordBox.Clear();
                    ConfirmPasswordBox.Clear();
                }
            }
        }
    }
}
