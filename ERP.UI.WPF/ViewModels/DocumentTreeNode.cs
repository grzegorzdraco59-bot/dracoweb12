using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// Węzeł drzewka dokumentów (Oferta → dokumenty).
/// FV: "KOŃCOWA ... | DO ZAPŁATY: {do_zaplaty_brutto}". FVZ/FPF/FVK: label + nr + data + brutto.
/// IsExpanded / IsSelected – do auto-rozwijania i auto-zaznaczania w TreeView.
/// </summary>
public class DocumentTreeNode : ViewModelBase
{
    private string _displayText = string.Empty;
    private bool _isExpanded;
    private bool _isSelected;

    public string DisplayText
    {
        get => _displayText;
        set => SetProperty(ref _displayText, value);
    }

    /// <summary>Węzeł rozwinięty – bind TreeViewItem.IsExpanded.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>Węzeł zaznaczony – bind TreeViewItem.IsSelected.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set => SetProperty(ref _isSelected, value);
    }

    public ObservableCollection<DocumentTreeNode> Children { get; } = new();

    /// <summary>FPF→PROFORMA, FVZ→ZALICZKA, FV→KOŃCOWA, FVK→KOREKTA.</summary>
    public static string GetLabelForDocType(string docType)
    {
        return docType?.ToUpperInvariant() switch
        {
            "FPF" => "PROFORMA",
            "FVZ" => "ZALICZKA",
            "FV" => "KOŃCOWA",
            "FVK" => "KOREKTA",
            _ => docType ?? ""
        };
    }
}
