using RestaurantApp.Models;
using RestaurantApp.Services;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class RegisterViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IDataService _dataService;

        private string _firstName;
        private string _lastName;
        private string _email;
        private string _phoneNumber;
        private string _deliveryAddress;
        private string _password;
        private string _confirmPassword;
        private bool _isLoading;
        private string _errorMessage;

        public string FirstName
        {
            get => _firstName;
            set => SetProperty(ref _firstName, value);
        }

        public string LastName
        {
            get => _lastName;
            set => SetProperty(ref _lastName, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string PhoneNumber
        {
            get => _phoneNumber;
            set => SetProperty(ref _phoneNumber, value);
        }

        public string DeliveryAddress
        {
            get => _deliveryAddress;
            set => SetProperty(ref _deliveryAddress, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public string ConfirmPassword
        {
            get => _confirmPassword;
            set => SetProperty(ref _confirmPassword, value);
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

        public ICommand RegisterCommand { get; }
        public ICommand LoginCommand { get; }
        public ICommand BackCommand { get; }

        public RegisterViewModel(INavigationService navigationService, IDataService dataService)
        {
            _navigationService = navigationService;
            _dataService = dataService;

            RegisterCommand = new AsyncRelayCommand(RegisterAsync, CanRegister);
            LoginCommand = new RelayCommand(() => _navigationService.NavigateTo<LoginViewModel>());
            BackCommand = new RelayCommand(() => _navigationService.NavigateTo<HomeViewModel>());
        }

        private bool CanRegister()
        {
            return !string.IsNullOrEmpty(FirstName) &&
                   !string.IsNullOrEmpty(LastName) &&
                   !string.IsNullOrEmpty(Email) &&
                   !string.IsNullOrEmpty(PhoneNumber) &&
                   !string.IsNullOrEmpty(DeliveryAddress) &&
                   !string.IsNullOrEmpty(Password) &&
                   Password == ConfirmPassword &&
                   IsValidEmail(Email) &&
                   IsValidPassword(Password) &&
                   !IsLoading;
        }

        private async Task RegisterAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                if (!ValidateInput())
                {
                    return;
                }

                string passwordHash = ComputeSha256Hash(Password);

                User newUser = new User
                {
                    FirstName = FirstName,
                    LastName = LastName,
                    Email = Email,
                    PhoneNumber = PhoneNumber,
                    DeliveryAddress = DeliveryAddress,
                    PasswordHash = passwordHash,
                    RoleId = 2 
                };

                int userId = await _dataService.RegisterUserAsync(newUser);

                if (userId > 0)
                {
                    var user = await _dataService.AuthenticateUserAsync(Email, passwordHash);
                    if (user != null)
                    {
                        AppSession.CurrentUser = user;
                       // _navigationService.NavigateTo<CustomerDashboardViewModel>();
                    }
                    else
                    {
                        MessageBox.Show("Registration successful! Please login with your credentials.", "Registration Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        _navigationService.NavigateTo<LoginViewModel>();
                    }
                }
                else
                {
                    ErrorMessage = "Registration failed. Please try again.";
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Registration failed: {ex.Message}";
            }
            finally
            {
                IsLoading = false;
            }
        }

        private bool ValidateInput()
        {
            if (!IsValidEmail(Email))
            {
                ErrorMessage = "Please enter a valid email address.";
                return false;
            }

            if (!IsValidPassword(Password))
            {
                ErrorMessage = "Password must be at least 8 characters long and contain at least one uppercase letter, one lowercase letter, one digit, and one special character.";
                return false;
            }

            if (Password != ConfirmPassword)
            {
                ErrorMessage = "Passwords do not match.";
                return false;
            }

            return true;
        }

        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return false;

            try
            {
                return new EmailAddressAttribute().IsValid(email);
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            var hasUppercase = new Regex(@"[A-Z]").IsMatch(password);
            var hasLowercase = new Regex(@"[a-z]").IsMatch(password);
            var hasDigit = new Regex(@"[0-9]").IsMatch(password);
            var hasSpecialChar = new Regex(@"[^a-zA-Z0-9]").IsMatch(password);

            return hasUppercase && hasLowercase && hasDigit && hasSpecialChar;
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