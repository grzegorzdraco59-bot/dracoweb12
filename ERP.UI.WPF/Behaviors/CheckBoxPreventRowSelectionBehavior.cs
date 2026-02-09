using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ERP.UI.WPF.Behaviors;

/// <summary>
/// Zapobiega zaznaczeniu wiersza DataGrid przy kliknięciu checkboxa.
/// Checkbox tylko toggle'uje IsSelected na elemencie – nie zmienia ActiveItem (SelectedItem).
/// </summary>
public static class CheckBoxPreventRowSelectionBehavior
{
    public static readonly DependencyProperty EnabledProperty =
        DependencyProperty.RegisterAttached(
            "Enabled",
            typeof(bool),
            typeof(CheckBoxPreventRowSelectionBehavior),
            new PropertyMetadata(false, OnEnabledChanged));

    public static bool GetEnabled(DependencyObject obj) => (bool)obj.GetValue(EnabledProperty);
    public static void SetEnabled(DependencyObject obj, bool value) => obj.SetValue(EnabledProperty, value);

    private static void OnEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DataGrid grid)
            return;

        if ((bool)e.NewValue)
            grid.PreviewMouseLeftButtonDown += Grid_PreviewMouseLeftButtonDown;
        else
            grid.PreviewMouseLeftButtonDown -= Grid_PreviewMouseLeftButtonDown;
    }

    private static void Grid_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        var source = e.OriginalSource as DependencyObject;
        if (source == null) return;

        // Sprawdź, czy kliknięto w CheckBox (lub wewnątrz niego)
        var checkBox = FindVisualParent<CheckBox>(source) ?? source as CheckBox;
        if (checkBox == null) return;

        // Znajdź wiersz i przełącz IsSelected na DataContext (SelectableItem)
        var row = FindVisualParent<DataGridRow>(source);
        if (row?.DataContext is not null)
        {
            var type = row.DataContext.GetType();
            var isSelectedProp = type.GetProperty("IsSelected");
            if (isSelectedProp != null)
            {
                var current = (bool)(isSelectedProp.GetValue(row.DataContext) ?? false);
                isSelectedProp.SetValue(row.DataContext, !current);
            }
            e.Handled = true;
        }
    }

    private static T? FindVisualParent<T>(DependencyObject? child) where T : DependencyObject
    {
        while (child != null)
        {
            if (child is T parent)
                return parent;
            child = VisualTreeHelper.GetParent(child);
        }
        return null;
    }
}
