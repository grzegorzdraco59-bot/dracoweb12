using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

public partial class KontrahentEditWindow : Window
{
    public KontrahentEditWindow(KontrahentEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.Saved += (_, _) => { DialogResult = true; Close(); };
        viewModel.Cancelled += (_, _) => { DialogResult = false; Close(); };
    }
}
