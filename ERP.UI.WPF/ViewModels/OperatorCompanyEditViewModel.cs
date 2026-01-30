using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Infrastructure.Repositories;
using MySqlConnector;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel okna edycji powiązania operator–firma.
/// NEW: Model z ustawionym UserId, reszta do wyboru. EDIT: Model wczytany GetByIdAsync(id).
/// </summary>
public class OperatorCompanyEditViewModel : ViewModelBase
{
    private readonly OperatorCompanyRepository _repository;

    public OperatorCompanyEditViewModel(OperatorCompanyRepository repository, OperatorCompanyDto model, bool isNew)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        Model = model ?? throw new ArgumentNullException(nameof(model));
        IsNew = isNew;

        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => Cancelled?.Invoke(this, EventArgs.Empty));
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    /// <summary>
    /// DTO: NEW = nowy z UserId ustawionym; EDIT = wczytany GetByIdAsync(id).
    /// Po Save w trybie NEW: Model.Id ustawione na zwrócone id.
    /// </summary>
    public OperatorCompanyDto Model { get; }

    public bool IsNew { get; }

    public string Title => IsNew ? "Dodawanie OperatorFirma" : "Edycja OperatorFirma";
    public string InfoMessage => IsNew ? "Nowe powiązanie operator–firma. Wypełnij dane i zapisz." : string.Empty;

    /// <summary>ID operatora (zapis do Model.UserId).</summary>
    public int UserId
    {
        get => Model.UserId;
        set { Model.UserId = value; OnPropertyChanged(); ((RelayCommand)SaveCommand).RaiseCanExecuteChanged(); }
    }

    /// <summary>ID firmy (zapis do Model.CompanyId).</summary>
    public int CompanyId
    {
        get => Model.CompanyId;
        set { Model.CompanyId = value; OnPropertyChanged(); ((RelayCommand)SaveCommand).RaiseCanExecuteChanged(); }
    }

    /// <summary>
    /// Tekstowa reprezentacja roli (pusta = brak roli). Zapis do Model.RoleId.
    /// </summary>
    public string RoleIdText
    {
        get => Model.RoleId?.ToString() ?? string.Empty;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                Model.RoleId = null;
            else if (int.TryParse(value, out var parsed))
                Model.RoleId = parsed;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanSave() => Model.UserId > 0 && Model.CompanyId > 0;

    private async Task SaveAsync()
    {
        try
        {
            if (Model.UserId <= 0 || Model.CompanyId <= 0)
            {
                System.Windows.MessageBox.Show(
                    "Wybierz operatora (UserId > 0) i firmę (CompanyId > 0).",
                    "Wymagane pola",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            var excludeId = IsNew ? null : (int?)Model.Id;
            var exists = await _repository.ExistsActiveByUserAndCompanyAsync(Model.UserId, Model.CompanyId, excludeId);
            if (exists)
            {
                System.Windows.MessageBox.Show(
                    "Powiązanie operator–firma już istnieje.",
                    "Unikalność",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            if (IsNew)
            {
                var newId = await _repository.AddAsync(Model);
                Model.Id = newId;
            }
            else
            {
                await _repository.UpdateAsync(Model);
            }
            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (MySqlException sqlEx)
        {
            var message = sqlEx.Number switch
            {
                1062 => "Powiązanie operator–firma już istnieje.",
                1451 => "Nie można wykonać operacji z powodu powiązań w bazie danych.",
                _ => $"Błąd bazy danych: {sqlEx.Message}"
            };
            System.Diagnostics.Trace.WriteLine($"OperatorCompanyEdit Save: {sqlEx}");
            System.Windows.MessageBox.Show(message, "Błąd", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.WriteLine($"OperatorCompanyEdit Save: {ex}");
            System.Windows.MessageBox.Show(
                "Wystąpił błąd podczas zapisu. Szczegóły w logu.",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }
}
