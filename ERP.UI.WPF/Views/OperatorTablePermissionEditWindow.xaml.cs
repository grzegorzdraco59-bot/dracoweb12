using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for OperatorTablePermissionEditWindow.xaml
/// </summary>
public partial class OperatorTablePermissionEditWindow : Window
{
    public OperatorTablePermissionEditWindow(OperatorTablePermissionEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        viewModel.CloseRequested += (sender, result) =>
        {
            DialogResult = result;
            Close();
        };
    }
}
