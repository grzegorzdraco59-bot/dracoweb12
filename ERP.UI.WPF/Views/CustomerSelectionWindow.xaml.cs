using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ERP.Application.DTOs;

namespace ERP.UI.WPF.Views;

/// <summary>
/// Interaction logic for CustomerSelectionWindow.xaml
/// </summary>
public partial class CustomerSelectionWindow : Window
{
    private readonly ObservableCollection<KontrahentLookupDto> _allCustomers;
    private readonly ObservableCollection<KontrahentLookupDto> _filteredCustomers;
    public KontrahentLookupDto? SelectedKontrahent { get; private set; }

    public CustomerSelectionWindow(ObservableCollection<KontrahentLookupDto> customers)
    {
        InitializeComponent();
        
        _allCustomers = new ObservableCollection<KontrahentLookupDto>(customers);
        _filteredCustomers = new ObservableCollection<KontrahentLookupDto>(customers);
        
        CustomersDataGrid.DataContext = _filteredCustomers;
        SelectButton.IsEnabled = false;
        
        // Ustawienie fokusa na pole wyszukiwania
        Loaded += (s, e) => SearchTextBox.Focus();
    }

    private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (sender is TextBox textBox)
        {
            FilterCustomers(textBox.Text);
        }
    }

    private void FilterCustomers(string searchText)
    {
        _filteredCustomers.Clear();
        
        if (string.IsNullOrWhiteSpace(searchText))
        {
            foreach (var customer in _allCustomers)
            {
                _filteredCustomers.Add(customer);
            }
        }
        else
        {
            var searchTextLower = searchText.ToLowerInvariant();
            foreach (var customer in _allCustomers)
            {
                // Szukaj po wszystkich kolumnach
                if (customer.Id.ToString().Contains(searchTextLower) ||
                    (customer.Nazwa?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (customer.Email?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (customer.Telefon?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (customer.Miasto?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (customer.Waluta?.ToLowerInvariant().Contains(searchTextLower) ?? false))
                {
                    _filteredCustomers.Add(customer);
                }
            }
        }
    }

    private void CustomersDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        SelectButton.IsEnabled = CustomersDataGrid.SelectedItem != null;
    }

    private void CustomersDataGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CustomersDataGrid.SelectedItem is KontrahentLookupDto customer)
        {
            SelectedKontrahent = customer;
            DialogResult = true;
            Close();
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (CustomersDataGrid.SelectedItem is KontrahentLookupDto customer)
        {
            SelectedKontrahent = customer;
            DialogResult = true;
            Close();
        }
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
