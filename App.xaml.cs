using System.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RestaurantApp.Services;
using RestaurantApp.ViewModels;
using Microsoft.EntityFrameworkCore;
using System.Configuration;
using System.Data;
using System.Windows;
using RestaurantApp.Services;
using RestaurantApp.ViewModels;
using RestaurantApp;
using Restaurant.Data;
using RestaurantApp.Data;
using Microsoft.Extensions.Logging;


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
        string connectionString = ConfigurationManager.ConnectionStrings["RestaurantDbConnection"].ConnectionString;

        services.AddDbContext<RestaurantDbContext>(options =>
                options.UseSqlServer(connectionString));

        services.AddSingleton<IDataService, DataService>();
        services.AddSingleton<INavigationService, NavigationService>(provider =>
            new NavigationService(type => (ViewModelBase)provider.GetRequiredService(type)));

        services.AddTransient<HomeViewModel>();
        services.AddTransient<LoginViewModel>();
        services.AddTransient<RegisterViewModel>();
        services.AddTransient<GuestViewModel>();
        services.AddTransient<CustomerDashboardViewModel>();
        services.AddTransient<MenuViewModel>();
        services.AddTransient<CartViewModel>();
        services.AddSingleton<MainWindowViewModel>();


        services.AddSingleton<MainWindow>();
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<RestaurantDbContext>();
            DbInitializer.Initialize(dbContext);
        }


        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.DataContext = _serviceProvider.GetRequiredService<MainWindowViewModel>();
        mainWindow.Show();
    }
}