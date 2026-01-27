using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for OrderPositionEditWindow.xaml
/// </summary>
public partial class OrderPositionEditWindow : Window
{
    public OrderPositionEditWindow(OrderPositionEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        viewModel.Saved += (s, e) => { DialogResult = true; Close(); };
        viewModel.Cancelled += (s, e) => { DialogResult = false; Close(); };
    }
}
