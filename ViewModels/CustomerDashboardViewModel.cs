using RestaurantApp.Models;
using RestaurantApp.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class CustomerDashboardViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IDataService _dataService;
        private ViewModelBase _currentView;
        private string _welcomeMessage;
        private bool _isLoading;

        public ViewModelBase CurrentView
        {
            get => _currentView;
            set => SetProperty(ref _currentView, value);
        }

        public string WelcomeMessage
        {
            get => _welcomeMessage;
            set => SetProperty(ref _welcomeMessage, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand ViewMenuCommand { get; }
        public ICommand ViewMyOrdersCommand { get; }
        public ICommand LogoutCommand { get; }
        public ICommand ViewCartCommand { get; }

        public CustomerDashboardViewModel(INavigationService navigationService, IDataService dataService)
        {
            _navigationService = navigationService;
            _dataService = dataService;

            WelcomeMessage = $"Welcome, {AppSession.CurrentUser?.FirstName} {AppSession.CurrentUser?.LastName}!";

            ViewMenuCommand = new RelayCommand(() => LoadView(new MenuViewModel(_navigationService, _dataService)));
            ViewMyOrdersCommand = new RelayCommand(() => LoadView(new CustomerOrdersViewModel(_navigationService, _dataService)));
            LogoutCommand = new RelayCommand(Logout);
            ViewCartCommand = new RelayCommand(() => LoadView(new CartViewModel(_navigationService, _dataService)));

            LoadView(new MenuViewModel(_navigationService, _dataService));
        }

        private void LoadView(ViewModelBase viewModel)
        {
            CurrentView = viewModel;
        }

        private void Logout()
        {
            AppSession.CurrentUser = null;
            _navigationService.NavigateTo<HomeViewModel>();
        }
    }
}