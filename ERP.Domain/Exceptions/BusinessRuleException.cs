namespace ERP.Domain.Exceptions;

/// <summary>
/// Wyjątek reguły biznesowej (np. edycja dokumentu w niedozwolonym statusie).
/// Zgodne z docs/FAZA4_STANY_DOKUMENTOW.md.
/// </summary>
public class BusinessRuleException : InvalidOperationException
{
    public BusinessRuleException(string message) : base(message) { }

    public BusinessRuleException(string message, Exception innerException)
        : base(message, innerException) { }
}
