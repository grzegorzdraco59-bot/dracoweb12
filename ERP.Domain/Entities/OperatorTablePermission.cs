namespace ERP.Domain.Entities;

/// <summary>
/// Encja reprezentująca uprawnienia operatora do poszczególnych tabel
/// Mapuje do tabeli: operator_table_permissions
/// </summary>
public class OperatorTablePermission : BaseEntity
{
    public int OperatorId { get; private set; }
    public string TableName { get; private set; }
    public bool CanSelect { get; private set; }
    public bool CanInsert { get; private set; }
    public bool CanUpdate { get; private set; }
    public bool CanDelete { get; private set; }

    // Konstruktor prywatny dla EF Core
    private OperatorTablePermission()
    {
        TableName = string.Empty;
    }

    // Główny konstruktor
    public OperatorTablePermission(int operatorId, string tableName, bool canSelect, bool canInsert, bool canUpdate, bool canDelete)
    {
        if (operatorId <= 0)
            throw new ArgumentException("Id operatora musi być większe od zera.", nameof(operatorId));
        if (string.IsNullOrWhiteSpace(tableName))
            throw new ArgumentException("Nazwa tabeli nie może być pusta.", nameof(tableName));

        OperatorId = operatorId;
        TableName = tableName;
        CanSelect = canSelect;
        CanInsert = canInsert;
        CanUpdate = canUpdate;
        CanDelete = canDelete;
    }

    public void UpdatePermissions(bool canSelect, bool canInsert, bool canUpdate, bool canDelete)
    {
        CanSelect = canSelect;
        CanInsert = canInsert;
        CanUpdate = canUpdate;
        CanDelete = canDelete;
        UpdateTimestamp();
    }
}
