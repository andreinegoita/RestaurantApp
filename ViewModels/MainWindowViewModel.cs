using RestaurantApp.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RestaurantApp.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;

        public ViewModelBase CurrentView => _navigationService.CurrentView;

        public MainWindowViewModel(INavigationService navigationService)
        {
            _navigationService = navigationService;
            _navigationService.PropertyChanged += (s, e) => OnPropertyChanged(nameof(CurrentView));

            _navigationService.NavigateTo<HomeViewModel>();
        }
    }
}
