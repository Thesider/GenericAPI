using GenericAPI.DTOs;
using GenericAPI.Models;

namespace GenericAPI.Services;

public interface IProductService
{
    Task<IEnumerable<ProductDto>> GetAllProductsAsync();
    Task<ProductDto?> GetProductByIdAsync(int id);
    Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm);
    Task<IEnumerable<ProductDto>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    Task<IEnumerable<ProductDto>> GetProductsInStockAsync();
    Task<IEnumerable<ProductDto>> GetProductsWithLowStockAsync(int threshold = 10);
    Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto);
    Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto);
    Task<bool> UpdateProductStockAsync(int id, UpdateProductStockDto updateStockDto);
    Task<bool> DeleteProductAsync(int id);
    Task<bool> ActivateProductAsync(int id);
    Task<bool> DeactivateProductAsync(int id);
    Task<(IEnumerable<ProductDto> Products, int TotalCount)> GetPagedProductsAsync(int page, int pageSize, string? searchTerm = null);
}
