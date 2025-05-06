using RestaurantApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;

        public ICommand LoginCommand { get; }
        public ICommand RegisterCommand { get; }
        public ICommand GuestCommand { get; }

        public HomeViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;

            LoginCommand = new RelayCommand(() => _navigationService.NavigateTo<LoginViewModel>());
            RegisterCommand = new RelayCommand(() => _navigationService.NavigateTo<RegisterViewModel>());
            GuestCommand = new RelayCommand(() => _navigationService.NavigateTo<GuestViewModel>());
        }
    }
}
