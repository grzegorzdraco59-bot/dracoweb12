using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for OrderMainEditWindow.xaml
/// </summary>
public partial class OrderMainEditWindow : Window
{
    public OrderMainEditWindow(OrderMainEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        viewModel.Saved += (s, e) => { DialogResult = true; Close(); };
        viewModel.Cancelled += (s, e) => { DialogResult = false; Close(); };
    }
}
