using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Repositories;
using ERP.UI.WPF.Views;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla widoku Pozycje Zamówienia
/// </summary>
public class OrderPositionsViewModel : ViewModelBase
{
    private readonly IOrderPositionMainRepository _repository;
    private string _searchText = string.Empty;
    private CollectionViewSource _positionsViewSource;
    private OrderPositionMainDto? _selectedPosition;
    
    public OrderPositionsViewModel(IOrderPositionMainRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        
        Positions = new ObservableCollection<OrderPositionMainDto>();
        
        _positionsViewSource = new CollectionViewSource { Source = Positions };
        _positionsViewSource.View.Filter = FilterPositions;
        
        LoadPositionsCommand = new RelayCommand(async () => await LoadPositionsAsync());
        
        AddPositionCommand = new RelayCommand(() => AddPosition());
        EditPositionCommand = new RelayCommand(() => EditPosition(), 
            () => SelectedPosition != null);
        DeletePositionCommand = new RelayCommand(async () => await DeletePositionAsync(), 
            () => SelectedPosition != null);
        
        // Automatyczne ładowanie przy starcie
        _ = LoadPositionsAsync();
    }

    public ObservableCollection<OrderPositionMainDto> Positions { get; }
    
    public ICollectionView FilteredPositions => _positionsViewSource.View;
    
    public OrderPositionMainDto? SelectedPosition
    {
        get => _selectedPosition;
        set
        {
            if (_selectedPosition != value)
            {
                _selectedPosition = value;
                OnPropertyChanged();
                if (EditPositionCommand is RelayCommand editCmd)
                    editCmd.RaiseCanExecuteChanged();
                if (DeletePositionCommand is RelayCommand deleteCmd)
                    deleteCmd.RaiseCanExecuteChanged();
            }
        }
    }
    
    public string SearchText
    {
        get => _searchText;
        set
        {
            if (_searchText != value)
            {
                _searchText = value;
                OnPropertyChanged();
                FilteredPositions.Refresh();
            }
        }
    }

    public ICommand LoadPositionsCommand { get; }
    public ICommand AddPositionCommand { get; }
    public ICommand EditPositionCommand { get; }
    public ICommand DeletePositionCommand { get; }

    private bool FilterPositions(object obj)
    {
        if (obj is not OrderPositionMainDto position)
            return false;
        
        if (string.IsNullOrWhiteSpace(SearchText))
            return true;
        
        var searchTextLower = SearchText.ToLowerInvariant();
        
        return (position.Id.ToString().Contains(searchTextLower)) ||
               (position.OrderId.ToString().Contains(searchTextLower)) ||
               (position.ProductId?.ToString().Contains(searchTextLower) ?? false) ||
               (position.ProductNameDraco?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (position.Product?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (position.ProductNameEng?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (position.OrderUnit?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (position.OrderQuantity?.ToString().Contains(searchTextLower) ?? false) ||
               (position.OrderPrice?.ToString().Contains(searchTextLower) ?? false) ||
               (position.ProductStatus?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (position.Supplier?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
               (position.Notes?.ToLowerInvariant().Contains(searchTextLower) ?? false);
    }
    
    private async Task LoadPositionsAsync()
    {
        try
        {
            var positions = await _repository.GetAllAsync();
            Positions.Clear();
            foreach (var position in positions)
            {
                Positions.Add(position);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show($"Błąd podczas ładowania pozycji zamówienia: {ex.Message}\n\n{ex.GetType().Name}\n\n{ex.StackTrace}", 
                "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
    }
    
    private void AddPosition()
    {
        try
        {
            var newPosition = new OrderPositionMainDto
            {
                Id = 0,
                CompanyId = 1, // TODO: Pobierz z sesji/kontekstu
                OrderId = 0
            };
            
            var editViewModel = new OrderPositionEditViewModel(_repository, newPosition);
            var editWindow = new OrderPositionEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                // Odświeżamy listę, aby pokazać zaktualizowane dane
                _ = LoadPositionsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas otwierania okna dodawania: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void EditPosition()
    {
        if (SelectedPosition == null) return;

        try
        {
            var editViewModel = new OrderPositionEditViewModel(_repository, SelectedPosition);
            var editWindow = new OrderPositionEditWindow(editViewModel)
            {
                Owner = System.Windows.Application.Current.MainWindow
            };

            if (editWindow.ShowDialog() == true)
            {
                // Odświeżamy listę, aby pokazać zaktualizowane dane
                _ = LoadPositionsAsync();
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas otwierania okna edycji: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private async Task DeletePositionAsync()
    {
        if (SelectedPosition == null) return;

        var result = System.Windows.MessageBox.Show(
            $"Czy na pewno chcesz usunąć pozycję zamówienia ID: {SelectedPosition.Id}?",
            "Potwierdzenie usunięcia",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Question);

        if (result == System.Windows.MessageBoxResult.Yes)
        {
            try
            {
                await _repository.DeleteAsync(SelectedPosition.Id);
                await LoadPositionsAsync();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Błąd podczas usuwania pozycji: {ex.Message}",
                    "Błąd",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}
