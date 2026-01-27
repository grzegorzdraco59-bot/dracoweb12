namespace ERP.Domain.Entities;

/// <summary>
/// Encja reprezentująca powiązanie użytkownika z firmą i rolą
/// Mapuje do tabeli: operatorfirma
/// </summary>
public class UserCompany : BaseEntity
{
    public int UserId { get; private set; }
    public int CompanyId { get; private set; }
    public int? RoleId { get; private set; }

    // Konstruktor prywatny dla EF Core
    private UserCompany()
    {
    }

    // Główny konstruktor
    public UserCompany(int userId, int companyId, int? roleId = null)
    {
        if (userId <= 0)
            throw new ArgumentException("Id użytkownika musi być większe od zera.", nameof(userId));
        if (companyId <= 0)
            throw new ArgumentException("Id firmy musi być większe od zera.", nameof(companyId));

        UserId = userId;
        CompanyId = companyId;
        RoleId = roleId;
    }

    public void UpdateRole(int? roleId)
    {
        RoleId = roleId;
        UpdateTimestamp();
    }
}
