using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ERP.UI.WPF.Models;

/// <summary>
/// Opakowanie elementu z flagą IsSelected do operacji masowych (checkbox).
/// ActiveItem (SelectedItem) pozostaje osobnym kontekstem dla szczegółów/edycji.
/// </summary>
public class SelectableItem<T> : INotifyPropertyChanged
{
    private T _item;
    private bool _isSelected;

    public SelectableItem(T item)
    {
        _item = item ?? throw new ArgumentNullException(nameof(item));
    }

    public T Item
    {
        get => _item;
        set
        {
            if (Equals(_item, value)) return;
            _item = value;
            OnPropertyChanged();
        }
    }

    /// <summary>Zaznaczenie checkboxa dla operacji masowych – nie zmienia ActiveItem.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
