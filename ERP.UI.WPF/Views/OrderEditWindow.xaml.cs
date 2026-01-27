using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for OrderEditWindow.xaml
/// </summary>
public partial class OrderEditWindow : Window
{
    public OrderEditWindow(OrderEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        viewModel.Saved += (sender, e) =>
        {
            DialogResult = true;
            Close();
        };
        
        viewModel.Cancelled += (sender, e) =>
        {
            DialogResult = false;
            Close();
        };
    }
}
