using System.Threading.Tasks;

namespace RestaurantApp.Services
{
    public interface IDialogService
    {
        Task<bool> ShowConfirmationAsync(string title, string message);
        Task ShowMessageAsync(string title, string message);
    }
}