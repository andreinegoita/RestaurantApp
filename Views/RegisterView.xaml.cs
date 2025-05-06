using RestaurantApp.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace RestaurantApp.Views
{
    public partial class RegisterView : UserControl
    {
        public RegisterView()
        {
            InitializeComponent();
            this.Loaded += RegisterView_Loaded;
        }

        private void RegisterView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is RegisterViewModel viewModel)
            {
                PasswordBox.PasswordChanged += (s, args) =>
                {
                    viewModel.Password = PasswordBox.Password;
                };

                ConfirmPasswordBox.PasswordChanged += (s, args) =>
                {
                    viewModel.ConfirmPassword = ConfirmPasswordBox.Password;
                };

                PasswordBox.Password = string.Empty;
                ConfirmPasswordBox.Password = string.Empty;
                viewModel.Password = string.Empty;
                viewModel.ConfirmPassword = string.Empty;
                viewModel.ErrorMessage = string.Empty;
            }
        }
    }
}