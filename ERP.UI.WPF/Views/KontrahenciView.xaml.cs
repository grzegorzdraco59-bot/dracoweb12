using System.Windows.Controls;
using System.Windows.Input;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

public partial class KontrahenciView : UserControl
{
    public KontrahenciView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
    }

    private void OnLoaded(object sender, System.Windows.RoutedEventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            SearchTextBox?.Focus();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    private void KontrahenciGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is not KontrahenciViewModel vm)
            return;
        if (!vm.IsSelectionMode)
            return;
        if (vm.SelectCommand.CanExecute(null))
            vm.SelectCommand.Execute(null);
    }
}
