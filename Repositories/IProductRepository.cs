using GenericAPI.Models;

namespace GenericAPI.Repositories;

public interface IProductRepository : IGenericRepository<Product>
{
    Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm);
    Task<IEnumerable<Product>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice);
    Task<IEnumerable<Product>> GetProductsInStockAsync();
    Task<IEnumerable<Product>> GetProductsWithLowStockAsync(int threshold = 10);
    Task<bool> UpdateStockAsync(int productId, int quantity);
    Task<Product?> GetProductWithOrderItemsAsync(int productId);
}
