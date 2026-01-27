namespace ERP.Domain.Entities;

/// <summary>
/// Encja domenowa reprezentująca rolę użytkownika
/// Mapuje do tabeli: rola
/// </summary>
public class Role : BaseEntity
{
    public string Name { get; private set; }

    // Konstruktor prywatny dla EF Core
    private Role()
    {
        Name = string.Empty;
    }

    // Główny konstruktor
    public Role(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Nazwa roli nie może być pusta.", nameof(name));

        Name = name;
    }

    public void UpdateName(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            throw new ArgumentException("Nazwa roli nie może być pusta.", nameof(newName));

        Name = newName;
        UpdateTimestamp();
    }
}
