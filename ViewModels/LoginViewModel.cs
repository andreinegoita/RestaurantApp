using RestaurantApp.Models;
using RestaurantApp.Services;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class LoginViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IDataService _dataService;
        private string _email;
        private string _password;
        private bool _isLoading;
        private string _errorMessage;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand BackCommand { get; }

        public LoginViewModel(INavigationService navigationService, IDataService dataService)
        {
            _navigationService = navigationService;
            _dataService = dataService;

            LoginCommand = new AsyncRelayCommand(AttemptLoginAsync, CanLogin);
            RegisterCommand = new RelayCommand(() => _navigationService.NavigateTo<RegisterViewModel>());
            BackCommand = new RelayCommand(() => _navigationService.NavigateTo<HomeViewModel>());
        }

        private bool CanLogin() => !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password) && !IsLoading;

        private async Task AttemptLoginAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                string passwordHash = ComputeSha256Hash(Password);
                var user = await _dataService.AuthenticateUserAsync(Email, passwordHash);

                if (user != null)
                {
                    AppSession.CurrentUser = user;

                    if (user.RoleId == 1) 
                    {
                        _navigationService.NavigateTo<AdminDashboardViewModel>();
                    }
                    else 
                    {
                        _navigationService.NavigateTo<CustomerDashboardViewModel>();
                    }
                }
                else
                {
                    ErrorMessage = "Invalid email or password. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Login failed: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                StringBuilder builder = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("x2"));
                }
                return builder.ToString();
            }
        }
    }
}