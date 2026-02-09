using System.Windows;

namespace ERP.UI.WPF.Views;

public partial class KontrahentTypeSelectWindow : Window
{
    public KontrahentTypeSelectWindow()
    {
        InitializeComponent();
    }

    public string SelectedType { get; private set; } = string.Empty;

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedType = "K";

        DialogResult = true;
    }
}
