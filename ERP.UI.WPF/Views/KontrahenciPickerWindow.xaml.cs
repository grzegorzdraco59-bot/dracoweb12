using System.Windows;
using ERP.Application.DTOs;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

public partial class KontrahenciPickerWindow : Window
{
    private readonly KontrahenciViewModel _vm;

    public KontrahentLookupDto? SelectedKontrahent { get; private set; }

    public KontrahenciPickerWindow(KontrahenciViewModel viewModel)
    {
        InitializeComponent();
        _vm = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
        _vm.IsSelectionMode = true;
        _vm.SelectionConfirmed += OnSelectionConfirmed;
        _vm.CloseRequested += (_, _) =>
        {
            DialogResult = false;
            Close();
        };
        DataContext = _vm;
    }

    private void OnSelectionConfirmed(object? sender, EventArgs e)
    {
        SelectedKontrahent = _vm.SelectedKontrahent;
        DialogResult = SelectedKontrahent != null;
        Close();
    }
}
