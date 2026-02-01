namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel dla okna edycji dokumentu faktury/proformy (minimalny – wyświetla Id).
/// </summary>
public class FakturaEditViewModel : ViewModelBase
{
    public FakturaEditViewModel(int invoiceId)
    {
        InvoiceId = invoiceId;
        WindowTitle = $"Proforma FPF – Id: {invoiceId}";
        InvoiceIdText = $"Id_faktury: {invoiceId}";
    }

    public int InvoiceId { get; }
    public string WindowTitle { get; }
    public string InvoiceIdText { get; }
}
