namespace ERP.Application.Validation;

/// <summary>
/// Wynik walidacji
/// </summary>
public class ValidationResult
{
    public bool IsValid => !Errors.Any();
    public List<string> Errors { get; } = new List<string>();

    public void AddError(string error)
    {
        if (!string.IsNullOrWhiteSpace(error))
            Errors.Add(error);
    }

    public void AddErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors)
            AddError(error);
    }
}

/// <summary>
/// Bazowa klasa dla walidatorów
/// </summary>
public abstract class BaseValidator<T>
{
    /// <summary>
    /// Waliduje obiekt
    /// </summary>
    public abstract ValidationResult Validate(T item);

    /// <summary>
    /// Waliduje obiekt asynchronicznie
    /// </summary>
    public virtual Task<ValidationResult> ValidateAsync(T item, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Validate(item));
    }

    /// <summary>
    /// Sprawdza czy wartość nie jest pusta
    /// </summary>
    protected bool IsNotEmpty(string? value, string fieldName, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result.AddError($"{fieldName} nie może być puste.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Sprawdza czy wartość nie jest null
    /// </summary>
    protected bool IsNotNull<TValue>(TValue? value, string fieldName, ValidationResult result) where TValue : class
    {
        if (value == null)
        {
            result.AddError($"{fieldName} nie może być null.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Sprawdza czy wartość jest większa od zera
    /// </summary>
    protected bool IsGreaterThanZero(int value, string fieldName, ValidationResult result)
    {
        if (value <= 0)
        {
            result.AddError($"{fieldName} musi być większe od zera.");
            return false;
        }
        return true;
    }

    /// <summary>
    /// Sprawdza długość stringa
    /// </summary>
    protected bool HasValidLength(string? value, int maxLength, string fieldName, ValidationResult result, int minLength = 0)
    {
        if (value == null)
            return true; // Null jest OK jeśli nie jest wymagane

        if (value.Length < minLength)
        {
            result.AddError($"{fieldName} musi mieć co najmniej {minLength} znaków.");
            return false;
        }

        if (value.Length > maxLength)
        {
            result.AddError($"{fieldName} nie może mieć więcej niż {maxLength} znaków.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Sprawdza format emaila
    /// </summary>
    protected bool IsValidEmail(string? email, string fieldName, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(email))
            return true; // Null jest OK jeśli nie jest wymagane

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            result.AddError($"{fieldName} ma nieprawidłowy format emaila.");
            return false;
        }
    }

    /// <summary>
    /// Sprawdza format NIP
    /// </summary>
    protected bool IsValidNip(string? nip, string fieldName, ValidationResult result)
    {
        if (string.IsNullOrWhiteSpace(nip))
            return true; // Null jest OK jeśli nie jest wymagane

        // Usuwamy spacje i myślniki
        var cleanNip = nip.Replace(" ", "").Replace("-", "");

        // NIP powinien mieć 10 cyfr
        if (cleanNip.Length != 10 || !cleanNip.All(char.IsDigit))
        {
            result.AddError($"{fieldName} musi składać się z 10 cyfr.");
            return false;
        }

        return true;
    }
}
