using myFridge.DTOs.Products;
namespace myFridge.Services.Interfaces;

public interface IProductService
{
    Task<List<ProductDto>> GetProductsAsync(string userId, ProductFilterDto filter);
    Task<ProductDto?> GetByIdAsync(Guid id);
    Task<ProductDto?> CreateAsync(CreateProductDto dto, string userId);
    Task<ProductDto?> UpdateAsync(Guid id, UpdateProductDto dto);
    Task<bool> DeleteSmartAsync(Guid id); 
    Task<bool> UpdateFavoriteAsync(Guid id, bool isFavorite);
}