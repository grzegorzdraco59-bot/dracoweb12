using System.Windows;
using System.Windows.Controls;
using ERP.UI.WPF.ViewModels;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for OffersView.xaml
/// </summary>
public partial class OffersView : UserControl
{
    public OffersView()
    {
        InitializeComponent();
    }

    private void OfferFlag_Changed(object sender, RoutedEventArgs e)
    {
        if (DataContext is OffersViewModel vm && vm.SaveOfferFlagsCommand.CanExecute(null))
            vm.SaveOfferFlagsCommand.Execute(null);
    }

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is OffersViewModel vm)
        {
            vm.RequestBringIntoView += OnRequestBringIntoView;
            vm.RequestScrollToSelectedOffer += OnRequestScrollToSelectedOffer;
        }
    }

    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is OffersViewModel vm)
        {
            vm.RequestBringIntoView -= OnRequestBringIntoView;
            vm.RequestScrollToSelectedOffer -= OnRequestScrollToSelectedOffer;
        }
    }

    private void OnRequestScrollToSelectedOffer(object? sender, EventArgs e)
    {
        if (DataContext is OffersViewModel vm && vm.SelectedOffer != null && OffersDataGrid != null)
        {
            Dispatcher.BeginInvoke(() =>
            {
                OffersDataGrid.ScrollIntoView(vm.SelectedOffer);
                // NIE wywołujemy Focus() – powodowało ucieczkę focusu z TextBoxa wyszukiwania
            }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
        }
    }

    private void OnRequestBringIntoView(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            BringSelectedNodeIntoView();
        }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private void BringSelectedNodeIntoView()
    {
        if (DocumentTreeView == null) return;
        var container = FindTreeViewItemWithSelectedNode(DocumentTreeView.ItemContainerGenerator, DocumentTreeView.Items);
        container?.BringIntoView();
    }

    private static TreeViewItem? FindTreeViewItemWithSelectedNode(ItemContainerGenerator generator, System.Collections.IEnumerable items)
    {
        if (items == null) return null;
        foreach (var item in items)
        {
            var container = generator.ContainerFromItem(item) as TreeViewItem;
            if (container != null)
            {
                if (container.DataContext is DocumentTreeNode node && node.IsSelected)
                    return container;
                if (container.ItemContainerGenerator != null && container.Items.Count > 0)
                {
                    var child = FindTreeViewItemWithSelectedNode(container.ItemContainerGenerator, container.Items);
                    if (child != null) return child;
                }
            }
        }
        return null;
    }
}
