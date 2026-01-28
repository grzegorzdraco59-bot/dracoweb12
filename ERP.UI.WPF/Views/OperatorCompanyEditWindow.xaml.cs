using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

public partial class OperatorCompanyEditWindow : Window
{
    public OperatorCompanyEditWindow(OperatorCompanyEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Saved += (_, _) => DialogResult = true;
        viewModel.Cancelled += (_, _) => DialogResult = false;
    }
}
