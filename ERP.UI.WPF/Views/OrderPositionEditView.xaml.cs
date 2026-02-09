using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for OrderPositionEditView.xaml
/// </summary>
public partial class OrderPositionEditView : Window
{
    public OrderPositionEditView(OrderPositionEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Saved += (_, _) => { DialogResult = true; Close(); };
        viewModel.Cancelled += (_, _) => { DialogResult = false; Close(); };
    }
}
