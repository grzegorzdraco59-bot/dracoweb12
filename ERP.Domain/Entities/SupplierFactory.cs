namespace ERP.Domain.Entities;

/// <summary>
/// Factory do tworzenia encji Supplier z danych z bazy
/// </summary>
public static class SupplierFactory
{
    public static Supplier FromDatabase(int id, int companyId, string name, string phone, string currency, string? email, string? notes)
    {
        var supplier = new Supplier(companyId, name, phone, currency);
        // Ustawiamy ID przez refleksję, ponieważ jest protected w BaseEntity
        var idProperty = typeof(BaseEntity).GetProperty("Id", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (idProperty != null && idProperty.CanWrite)
        {
            idProperty.SetValue(supplier, id);
        }
        
        if (email != null) supplier.UpdateContactInfo(email, phone);
        if (notes != null) supplier.UpdateNotes(notes);
        
        return supplier;
    }
}
