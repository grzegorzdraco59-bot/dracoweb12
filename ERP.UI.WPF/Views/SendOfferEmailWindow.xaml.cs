using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

public partial class SendOfferEmailWindow : Window
{
    public SendOfferEmailWindow(SendOfferEmailViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        viewModel.OnSendRequested = () =>
        {
            DialogResult = true;
            Close();
        };
        viewModel.OnCancelRequested = () =>
        {
            DialogResult = false;
            Close();
        };
    }
}
