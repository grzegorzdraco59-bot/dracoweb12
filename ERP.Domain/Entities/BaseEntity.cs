using System.ComponentModel.DataAnnotations;

namespace ERP.Domain.Entities;

/// <summary>
/// Klasa bazowa dla wszystkich encji domenowych
/// </summary>
public abstract class BaseEntity
{
    [Key]
    public int Id { get; protected set; }
    
    public DateTime CreatedAt { get; protected set; }
    
    public DateTime? UpdatedAt { get; protected set; }
    
    protected BaseEntity()
    {
        CreatedAt = DateTime.UtcNow;
    }
    
    protected void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}