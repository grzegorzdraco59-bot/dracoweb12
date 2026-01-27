using System.Windows;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for OfferPositionEditWindow.xaml
/// </summary>
public partial class OfferPositionEditWindow : Window
{
    public OfferPositionEditWindow(OfferPositionEditViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Zamknij okno po zapisaniu
        viewModel.Saved += (sender, e) => DialogResult = true;
        viewModel.Cancelled += (sender, e) => DialogResult = false;
    }
}
