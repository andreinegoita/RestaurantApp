using System.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApp.Services;
using RestaurantApp.ViewModels;
using System.Configuration;
using System.Data;
using System.Windows;
using RestaurantApp.Services;
using RestaurantApp.ViewModels;
using RestaurantApp;
 

namespace RestaurantApp;

public partial class App : Application
{
    private readonly Microsoft.Extensions.DependencyInjection.ServiceProvider _serviceProvider;

    public App()
    {
        ServiceCollection services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();
    }

    private void ConfigureServices(ServiceCollection services)
    {
        services.AddSingleton<INavigationService>(provider =>
            new NavigationService(viewModelType => (ViewModelBase)provider.GetRequiredService(viewModelType)));


        services.AddTransient<HomeViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<GuestViewModel>();
        services.AddSingleton<MainWindowViewModel>();


        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.Show();
    }
}