using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace ERP.UI.WPF;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var app = System.Windows.Application.Current as App;
        var userContext = app?.GetUserContext();
        if (userContext == null || !userContext.IsLoggedIn || !userContext.CompanyId.HasValue)
        {
            MessageBox.Show("Brak wybranej firmy. Zaloguj się ponownie i wybierz firmę.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            Close();
            return;
        }
    }
}