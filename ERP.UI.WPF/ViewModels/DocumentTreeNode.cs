using System.Collections.ObjectModel;

namespace ERP.UI.WPF.ViewModels;

/// <summary>
/// Węzeł drzewka dokumentów (Oferta → dokumenty).
/// FV: "KOŃCOWA {doc_full_no} | {data} | {sum_brutto} | DO ZAPŁATY: {do_zaplaty_brutto}".
/// FVZ: "ZALICZKA {doc_full_no} | {data} | {sum_brutto}". Pozostałe (FPF/FVK): label + nr + data + brutto.
/// Kwoty z nagłówków (faktury), nie liczone w UI.
/// </summary>
public class DocumentTreeNode
{
    public string DisplayText { get; set; } = string.Empty;
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
