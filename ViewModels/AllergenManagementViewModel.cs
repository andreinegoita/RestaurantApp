using RestaurantApp.Models;
using RestaurantApp.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RestaurantApp.ViewModels
{
    public class AllergenManagementViewModel : ViewModelBase
    {
        private readonly INavigationService _navigationService;
        private readonly IDataService _dataService;

        private ObservableCollection<Allergen> _allergens;
        private Allergen _selectedAllergen;
        private Allergen _newAllergen;
        private bool _isLoading;
        private bool _isEditing;
        private bool _isAdding;
        private string _errorMessage;

        public ObservableCollection<Allergen> Allergens
        {
            get => _allergens;
            set => SetProperty(ref _allergens, value);
        }

        public Allergen SelectedAllergen
        {
            get => _selectedAllergen;
            set
            {
                if (SetProperty(ref _selectedAllergen, value) && value != null)
                {
                    NewAllergen = new Allergen
                    {
                        AllergenId = value.AllergenId,
                        Name = value.Name,
                        Description = value.Description
                    };
                    IsEditing = true;
                    IsAdding = false;
                }
            }
        }

        public Allergen NewAllergen
        {
            get => _newAllergen;
            set => SetProperty(ref _newAllergen, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsEditing
        {
            get => _isEditing;
            set => SetProperty(ref _isEditing, value);
        }

        public bool IsAdding
        {
            get => _isAdding;
            set => SetProperty(ref _isAdding, value);
        }

        public string ErrorMessage
        {
            get => _errorMessage;
            set => SetProperty(ref _errorMessage, value);
        }

        public ICommand LoadAllergensCommand { get; }
        public ICommand AddNewAllergenCommand { get; }
        public ICommand SaveAllergenCommand { get; }
        public ICommand DeleteAllergenCommand { get; }
        public ICommand CancelEditCommand { get; }
        public ICommand BackCommand { get; }

        public AllergenManagementViewModel(INavigationService navigationService, IDataService dataService)
        {
            _navigationService = navigationService;
            _dataService = dataService;

            Allergens = new ObservableCollection<Allergen>();
            NewAllergen = new Allergen();

            LoadAllergensCommand = new AsyncRelayCommand(LoadAllergensAsync);
            AddNewAllergenCommand = new RelayCommand(InitializeNewAllergen);
            SaveAllergenCommand = new AsyncRelayCommand(SaveAllergenAsync);
            DeleteAllergenCommand = new AsyncRelayCommand(DeleteAllergenAsync);
            CancelEditCommand = new RelayCommand(CancelEdit);
            BackCommand = new RelayCommand(GoBack);

            Task.Run(() => LoadAllergensAsync());
        }

        private async Task LoadAllergensAsync()
        {
            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                var allergens = await _dataService.GetAllergensAsync();
                Allergens = new ObservableCollection<Allergen>(allergens);
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error loading allergens: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void InitializeNewAllergen()
        {
            SelectedAllergen = null;
            NewAllergen = new Allergen();
            IsAdding = true;
            IsEditing = false;
            ErrorMessage = string.Empty;
        }

        private async Task SaveAllergenAsync()
        {
            if (string.IsNullOrWhiteSpace(NewAllergen.Name))
            {
                ErrorMessage = "Allergen name is required.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                if (IsAdding)
                {
                    int newId = await _dataService.AddAllergenAsync(NewAllergen);
                    NewAllergen.AllergenId = newId;
                    Allergens.Add(NewAllergen);
                }
                else if (IsEditing)
                {
                    await _dataService.UpdateAllergenAsync(NewAllergen);

                    int index = Allergens.ToList().FindIndex(a => a.AllergenId == NewAllergen.AllergenId);
                    if (index >= 0)
                    {
                        Allergens[index] = NewAllergen;
                    }
                }

                IsAdding = false;
                IsEditing = false;
                NewAllergen = new Allergen();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error saving allergen: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task DeleteAllergenAsync()
        {
            if (SelectedAllergen == null)
            {
                ErrorMessage = "No allergen selected for deletion.";
                return;
            }

            try
            {
                IsLoading = true;
                ErrorMessage = string.Empty;

                await _dataService.DeleteAllergenAsync(SelectedAllergen.AllergenId);

                Allergens.Remove(SelectedAllergen);
                SelectedAllergen = null;
                IsEditing = false;
                NewAllergen = new Allergen();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Error deleting allergen: {ex.Message}";
                System.Diagnostics.Debug.WriteLine(ErrorMessage);
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void CancelEdit()
        {
            IsAdding = false;
            IsEditing = false;
            NewAllergen = new Allergen();
            SelectedAllergen = null;
            ErrorMessage = string.Empty;
        }

        private void GoBack()
        {
            _navigationService.NavigateTo<AdminDashboardViewModel>();
        }
    }
}