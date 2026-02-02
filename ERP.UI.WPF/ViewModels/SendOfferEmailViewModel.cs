using System.Windows;
using System.Windows.Input;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// ViewModel okna "Wyślij ofertę" – pola Do, DW/UDW, Temat, Treść, checkbox zmiany statusu.
/// Walidacja: Do niepuste i w formacie e-mail. Wyślij zamyka okno z DialogResult=true po walidacji.
/// </summary>
public class SendOfferEmailViewModel : ViewModelBase
{
    private string _to = "";
    private string _dwUdw = "";
    private string _subject = "";
    private string _body = "";
    private bool _changeStatusAfterSend = true;
    private string _validationError = "";

    public SendOfferEmailViewModel(string defaultTo, string defaultSubject, string defaultBody)
    {
        _to = defaultTo ?? "";
        _subject = defaultSubject ?? "";
        _body = defaultBody ?? "";

        SendCommand = new RelayCommand(ExecuteSend, () => true);
        CancelCommand = new RelayCommand(() => OnCancelRequested?.Invoke(), () => true);
    }

    public string To
    {
        get => _to;
        set { _to = value ?? ""; OnPropertyChanged(); ClearValidation(); }
    }

    public string DwUdw
    {
        get => _dwUdw;
        set { _dwUdw = value ?? ""; OnPropertyChanged(); }
    }

    public string Subject
    {
        get => _subject;
        set { _subject = value ?? ""; OnPropertyChanged(); }
    }

    public string Body
    {
        get => _body;
        set { _body = value ?? ""; OnPropertyChanged(); }
    }

    public bool ChangeStatusAfterSend
    {
        get => _changeStatusAfterSend;
        set { _changeStatusAfterSend = value; OnPropertyChanged(); }
    }

    public string ValidationError
    {
        get => _validationError;
        set { _validationError = value ?? ""; OnPropertyChanged(); OnPropertyChanged(nameof(ValidationErrorVisibility)); }
    }

    public Visibility ValidationErrorVisibility => string.IsNullOrEmpty(_validationError) ? Visibility.Collapsed : Visibility.Visible;

    public ICommand SendCommand { get; }
    public ICommand CancelCommand { get; }

    internal Action? OnSendRequested { get; set; }
    internal Action? OnCancelRequested { get; set; }

    private void ClearValidation() => ValidationError = "";

    private static bool LooksLikeEmail(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        s = s.Trim();
        var at = s.IndexOf('@');
        if (at <= 0 || at >= s.Length - 1) return false;
        var dot = s.IndexOf('.', at + 1);
        return dot > at + 1 && dot < s.Length - 1;
    }

    private void ExecuteSend()
    {
        System.Windows.MessageBox.Show("MAIL: klik działa", "Oferta mail", System.Windows.MessageBoxButton.OK);
        var toTrim = To?.Trim() ?? "";
        if (string.IsNullOrEmpty(toTrim))
        {
            ValidationError = "Pole 'Do' nie może być puste.";
            return;
        }
        if (!LooksLikeEmail(toTrim))
        {
            ValidationError = "Pole 'Do' musi zawierać poprawny adres e-mail (np. adres@domena.pl).";
            return;
        }
        ValidationError = "";
        OnSendRequested?.Invoke();
    }
}
