using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Okno edycji dokumentu z tabeli faktury (proforma FPF itd.).
/// </summary>
public partial class FakturaEditWindow : Window
{
    public FakturaEditWindow(FakturaEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
