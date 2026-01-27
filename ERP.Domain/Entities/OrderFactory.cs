using System.Reflection;

namespace ERP.Domain.Entities;

/// <summary>
/// Fabryka do tworzenia Order z danych z bazy danych
/// </summary>
public static class OrderFactory
{
    public static Order FromDatabase(
        int id,
        int companyId,
        int? supplierId,
        string? supplierName,
        int? status,
        DateTime? orderDate,
        string? orderNumber,
        string? notes,
        decimal? totalAmount,
        string? productNameDraco,
        decimal? quantity,
        string? salesUnit,
        DateTime createdAt,
        DateTime? updatedAt)
    {
        var order = new Order(companyId);
        
        // Używamy refleksji do ustawienia Id i innych właściwości
        SetProperty(order, "Id", id);
        SetProperty(order, "SupplierId", supplierId);
        SetProperty(order, "SupplierName", supplierName);
        SetProperty(order, "Status", status);
        SetProperty(order, "OrderDate", orderDate);
        SetProperty(order, "OrderNumber", orderNumber);
        SetProperty(order, "Notes", notes);
        SetProperty(order, "TotalAmount", totalAmount);
        SetProperty(order, "ProductNameDraco", productNameDraco);
        SetProperty(order, "Quantity", quantity);
        SetProperty(order, "SalesUnit", salesUnit);
        SetProperty(order, "CreatedAt", createdAt);
        SetProperty(order, "UpdatedAt", updatedAt);
        
        return order;
    }

    private static void SetProperty(object obj, string propertyName, object? value)
    {
        var property = obj.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        if (property != null && property.CanWrite)
        {
            if (value == null && property.PropertyType.IsValueType && Nullable.GetUnderlyingType(property.PropertyType) == null)
            {
                // Nie ustawiamy null dla wartościowych typów, które nie są nullable
                return;
            }
            
            var convertedValue = value == null ? null : Convert.ChangeType(value, Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType);
            property.SetValue(obj, convertedValue);
        }
    }
}
