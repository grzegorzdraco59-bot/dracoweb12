using ERP.Application.DTOs;

namespace ERP.UI.WPF.Services;

public interface ITowarPicker
{
    Task<ProductDto?> PickAsync(int companyId);
}
