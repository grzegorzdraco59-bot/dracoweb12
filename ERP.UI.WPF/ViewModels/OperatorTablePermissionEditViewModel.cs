using System.Collections.ObjectModel;
using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Application.Services;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji uprawnień operatora do tabel
/// </summary>
public class OperatorTablePermissionEditViewModel : ViewModelBase
{
    private readonly IOperatorPermissionService _permissionService;
    private readonly OperatorTablePermissionDto _permission;
    private readonly bool _isNew;

    private string _tableName = string.Empty;
    private bool _canSelect;
    private bool _canInsert;
    private bool _canUpdate;
    private bool _canDelete;

    public OperatorTablePermissionEditViewModel(
        IOperatorPermissionService permissionService,
        OperatorTablePermissionDto permission,
        bool isNew)
    {
        _permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
        _permission = permission ?? throw new ArgumentNullException(nameof(permission));
        _isNew = isNew;

        AvailableTables = new ObservableCollection<string>();
        TableName = permission.TableName;
        CanSelect = permission.CanSelect;
        CanInsert = permission.CanInsert;
        CanUpdate = permission.CanUpdate;
        CanDelete = permission.CanDelete;

        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => Cancel());

        // Załaduj listę dostępnych tabel
        _ = LoadAvailableTablesAsync();
    }

    public string WindowTitle => _isNew ? "Dodaj uprawnienie" : "Edytuj uprawnienie";
    public bool IsNew => _isNew;
    public int OperatorId => _permission.OperatorId;
    public string OperatorName => _permission.OperatorName;
    public ObservableCollection<string> AvailableTables { get; }

    public string TableName
    {
        get => _tableName;
        set
        {
            SetProperty(ref _tableName, value);
            if (SaveCommand is RelayCommand saveCmd)
                saveCmd.RaiseCanExecuteChanged();
        }
    }

    public bool CanSelect
    {
        get => _canSelect;
        set => SetProperty(ref _canSelect, value);
    }

    public bool CanInsert
    {
        get => _canInsert;
        set => SetProperty(ref _canInsert, value);
    }

    public bool CanUpdate
    {
        get => _canUpdate;
        set => SetProperty(ref _canUpdate, value);
    }

    public bool CanDelete
    {
        get => _canDelete;
        set => SetProperty(ref _canDelete, value);
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    public event EventHandler<bool>? CloseRequested;

    private bool CanSave()
    {
        return !string.IsNullOrWhiteSpace(TableName);
    }

    private async Task SaveAsync()
    {
        try
        {
            if (_isNew)
            {
                await _permissionService.SetPermissionAsync(
                    OperatorId,
                    TableName,
                    CanSelect,
                    CanInsert,
                    CanUpdate,
                    CanDelete);
            }
            else
            {
                await _permissionService.UpdatePermissionAsync(
                    _permission.Id,
                    CanSelect,
                    CanInsert,
                    CanUpdate,
                    CanDelete);
            }

            CloseRequested?.Invoke(this, true);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas zapisywania uprawnienia: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void Cancel()
    {
        CloseRequested?.Invoke(this, false);
    }

    private async Task LoadAvailableTablesAsync()
    {
        try
        {
            AvailableTables.Clear();
            var tables = await _permissionService.GetAvailableTablesAsync();
            foreach (var table in tables.OrderBy(t => t))
            {
                AvailableTables.Add(table);
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas ładowania listy tabel: {ex.Message}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }
}
