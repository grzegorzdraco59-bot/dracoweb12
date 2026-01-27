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
    private readonly ObservableCollection<CustomerDto> _allCustomers;
    private readonly ObservableCollection<CustomerDto> _filteredCustomers;
    public CustomerDto? SelectedCustomer { get; private set; }

    public CustomerSelectionWindow(ObservableCollection<CustomerDto> customers)
    {
        InitializeComponent();
        
        _allCustomers = new ObservableCollection<CustomerDto>(customers);
        _filteredCustomers = new ObservableCollection<CustomerDto>(customers);
        
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
                    (customer.Name?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (customer.Nip?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (customer.City?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (customer.Email1?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (customer.Street?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (customer.PostalCode?.ToLowerInvariant().Contains(searchTextLower) ?? false) ||
                    (customer.Country?.ToLowerInvariant().Contains(searchTextLower) ?? false))
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
        if (CustomersDataGrid.SelectedItem is CustomerDto customer)
        {
            SelectedCustomer = customer;
            DialogResult = true;
            Close();
        }
    }

    private void SelectButton_Click(object sender, RoutedEventArgs e)
    {
        if (CustomersDataGrid.SelectedItem is CustomerDto customer)
        {
            SelectedCustomer = customer;
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
