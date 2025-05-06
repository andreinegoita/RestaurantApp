using RestaurantApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class GuestViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;

        public ICommand BackCommand { get; }

        public GuestViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            BackCommand = new RelayCommand(() => _navigationService.NavigateTo<HomeViewModel>());
        }
    }
}
