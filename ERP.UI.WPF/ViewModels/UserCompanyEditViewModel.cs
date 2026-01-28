using System.Windows.Input;
using ERP.Application.DTOs;
using ERP.Domain.Entities;
using ERP.Domain.Repositories;
using ERP.Infrastructure.Services;
using MySqlConnector;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji operatorfirma
/// </summary>
public class UserCompanyEditViewModel : ViewModelBase
{
    private readonly IUserCompanyRepository _repository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly UserCompanyDto _originalDto;
    private readonly bool _isNew;
    
    private int _userId;
    private int _companyId;
    private int? _roleId;
    private string _roleIdText = string.Empty;

    public UserCompanyEditViewModel(IUserCompanyRepository repository, IUnitOfWork unitOfWork, UserCompanyDto userCompanyDto, bool isNew = false)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _originalDto = userCompanyDto ?? throw new ArgumentNullException(nameof(userCompanyDto));
        _isNew = isNew;
        
        _userId = userCompanyDto.UserId;
        _companyId = userCompanyDto.CompanyId;
        _roleId = userCompanyDto.RoleId;
        _roleIdText = userCompanyDto.RoleId?.ToString() ?? string.Empty;
        
        SaveCommand = new RelayCommand(async () => await SaveAsync(), () => CanSave());
        CancelCommand = new RelayCommand(() => OnCancelled());
    }

    public event EventHandler? Saved;
    public event EventHandler? Cancelled;

    public int Id => _originalDto.Id;
    public bool IsNew => _isNew;

    /// <summary>
    /// Informacja gdy brak powiązania (tryb dodawania) – wyświetlana opcjonalnie w UI.
    /// </summary>
    public string InfoMessage => _isNew ? "Brak powiązania operator–firma. Zostanie utworzone przy zapisie." : string.Empty;
    
    public string Title => _isNew ? "Dodawanie OperatorFirma" : "Edycja OperatorFirma";

    public int UserId
    {
        get => _userId;
        set
        {
            _userId = value;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public int CompanyId
    {
        get => _companyId;
        set
        {
            _companyId = value;
            OnPropertyChanged();
            ((RelayCommand)SaveCommand).RaiseCanExecuteChanged();
        }
    }

    public int? RoleId
    {
        get => _roleId;
        private set
        {
            _roleId = value;
            OnPropertyChanged();
            RoleIdText = value?.ToString() ?? string.Empty;
        }
    }

    public string RoleIdText
    {
        get => _roleIdText;
        set
        {
            _roleIdText = value;
            OnPropertyChanged();
            
            // Konwersja tekstu na int?
            if (string.IsNullOrWhiteSpace(value))
            {
                _roleId = null;
            }
            else if (int.TryParse(value, out int parsedValue))
            {
                _roleId = parsedValue;
            }
        }
    }

    public ICommand SaveCommand { get; }
    public ICommand CancelCommand { get; }

    private bool CanSave()
    {
        return UserId > 0 && CompanyId > 0;
    }

    private async Task SaveAsync()
    {
        try
        {
            if (_isNew)
            {
                // Sprawdzamy czy taki rekord już istnieje
                var existing = await _repository.GetByUserAndCompanyAsync(UserId, CompanyId);
                if (existing != null)
                {
                    System.Windows.MessageBox.Show(
                        $"Operator o ID {UserId} jest już powiązany z firmą o ID {CompanyId}.",
                        "Rekord już istnieje",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // Tworzymy nowy UserCompany
                var newUserCompany = new UserCompany(UserId, CompanyId, RoleId);
                var id = await _repository.AddAsync(newUserCompany);
            }
            else
            {
                // Pobieramy istniejący UserCompany
                var existingUserCompany = await _repository.GetByIdAsync(_originalDto.Id);
                if (existingUserCompany == null)
                {
                    // Rekord nie istnieje (np. usunięty) – traktujemy jak dodawanie powiązania
                    var existing = await _repository.GetByUserAndCompanyAsync(UserId, CompanyId);
                    if (existing != null)
                    {
                        System.Windows.MessageBox.Show(
                            $"Operator o ID {UserId} jest już powiązany z firmą o ID {CompanyId}.",
                            "Rekord już istnieje",
                            System.Windows.MessageBoxButton.OK,
                            System.Windows.MessageBoxImage.Warning);
                        return;
                    }
                    await _unitOfWork.ExecuteInTransactionAsync(async (transaction) =>
                    {
                        var conn = transaction.Connection ?? throw new InvalidOperationException("DatabaseContext returned null connection.");
                        using (var cmdIns = new MySqlCommand(
                            "INSERT INTO operatorfirma (id_operatora, id_firmy, rola) VALUES (@UserId, @CompanyId, @RoleId)",
                            conn, transaction))
                        {
                            cmdIns.Parameters.AddWithValue("@UserId", UserId);
                            cmdIns.Parameters.AddWithValue("@CompanyId", CompanyId);
                            cmdIns.Parameters.AddWithValue("@RoleId", RoleId ?? (object)DBNull.Value);
                            await cmdIns.ExecuteNonQueryAsync();
                        }
                    });
                }
                else
                {
                // Aktualizujemy właściwości
                // UserCompany ma tylko metodę UpdateRole, więc musimy utworzyć nowy obiekt jeśli się zmieniły UserId lub CompanyId
                if (existingUserCompany.UserId != UserId || existingUserCompany.CompanyId != CompanyId)
                {
                    // DELETE + INSERT w jednej transakcji – przy wyjątku rollback, stary rekord pozostaje
                    await _unitOfWork.ExecuteInTransactionAsync(async (transaction) =>
                    {
                        var conn = transaction.Connection ?? throw new InvalidOperationException("DatabaseContext returned null connection.");
                        using (var cmdDel = new MySqlCommand("DELETE FROM operatorfirma WHERE id = @Id", conn, transaction))
                        {
                            cmdDel.Parameters.AddWithValue("@Id", _originalDto.Id);
                            await cmdDel.ExecuteNonQueryAsync();
                        }
                        using (var cmdIns = new MySqlCommand(
                            "INSERT INTO operatorfirma (id_operatora, id_firmy, rola) VALUES (@UserId, @CompanyId, @RoleId)",
                            conn, transaction))
                        {
                            cmdIns.Parameters.AddWithValue("@UserId", UserId);
                            cmdIns.Parameters.AddWithValue("@CompanyId", CompanyId);
                            cmdIns.Parameters.AddWithValue("@RoleId", RoleId ?? (object)DBNull.Value);
                            await cmdIns.ExecuteNonQueryAsync();
                        }
                    });
                }
                else
                {
                    // Jeśli tylko rola się zmieniła, możemy użyć UpdateRole
                    existingUserCompany.UpdateRole(RoleId);
                    await _repository.UpdateAsync(existingUserCompany);
                }
                }
            }

            Saved?.Invoke(this, EventArgs.Empty);
        }
        catch (MySqlConnector.MySqlException sqlEx)
        {
            string errorMessage = $"Błąd podczas zapisywania operatorfirma:\n\n{sqlEx}";
            if (sqlEx.Number == 1062)
                errorMessage = $"Rekord już istnieje: Operator o ID {UserId} jest już powiązany z firmą o ID {CompanyId}.\n\n{sqlEx}";
            else if (sqlEx.Number == 1452)
                errorMessage = $"Błąd klucza obcego: Operator o ID {UserId} lub firma o ID {CompanyId} nie istnieje.\n\n{sqlEx}";
            System.Windows.MessageBox.Show(
                errorMessage,
                "Błąd bazy danych",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd podczas zapisywania operatorfirma:\n\n{ex.ToString()}",
                "Błąd",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
    }

    private void OnCancelled()
    {
        Cancelled?.Invoke(this, EventArgs.Empty);
    }
}
