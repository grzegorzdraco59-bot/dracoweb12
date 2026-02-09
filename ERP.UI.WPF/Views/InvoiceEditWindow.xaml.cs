using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for InvoiceEditWindow.xaml
/// </summary>
public partial class InvoiceEditWindow : Window
{
    public InvoiceEditWindow(InvoiceEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Saved += (_, _) => { DialogResult = true; Close(); };
        viewModel.Cancelled += (_, _) => { DialogResult = false; Close(); };
    }
}
