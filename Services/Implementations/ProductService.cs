using AutoMapper;
using GenericAPI.DTOs;
using GenericAPI.Middleware;
using GenericAPI.Models;
using GenericAPI.Repositories;
using Microsoft.EntityFrameworkCore;

namespace GenericAPI.Services;

public class ProductService : IProductService
{
    
    private readonly IProductRepository _productRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<ProductService> _logger;

    public ProductService(IProductRepository productRepository, IMapper mapper, ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IEnumerable<ProductDto>> GetAllProductsAsync()
    {
        try
        {
            var products = await _productRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all products");
            throw;
        }
    }

    public async Task<ProductDto?> GetProductByIdAsync(int id)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(id);
            return product != null ? _mapper.Map<ProductDto>(product) : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID {ProductId}", id);
            throw;
        }
    }

    public async Task<IEnumerable<ProductDto>> SearchProductsAsync(string searchTerm)
    {
        try
        {
            var products = await _productRepository.SearchProductsAsync(searchTerm);
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with term {SearchTerm}", searchTerm);
            throw;
        }
    }

    public async Task<IEnumerable<ProductDto>> GetProductsByPriceRangeAsync(decimal minPrice, decimal maxPrice)
    {
        try
        {
            if (minPrice < 0 || maxPrice < 0 || minPrice > maxPrice)
                throw new ValidationException("Invalid price range");

            var products = await _productRepository.GetProductsByPriceRangeAsync(minPrice, maxPrice);
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products by price range {MinPrice}-{MaxPrice}", minPrice, maxPrice);
            throw;
        }
    }

    public async Task<IEnumerable<ProductDto>> GetProductsInStockAsync()
    {
        try
        {
            var products = await _productRepository.GetProductsInStockAsync();
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products in stock");
            throw;
        }
    }

    public async Task<IEnumerable<ProductDto>> GetProductsWithLowStockAsync(int threshold = 10)
    {
        try
        {
            var products = await _productRepository.GetProductsWithLowStockAsync(threshold);
            return _mapper.Map<IEnumerable<ProductDto>>(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products with low stock (threshold: {Threshold})", threshold);
            throw;
        }
    }

    public async Task<ProductDto> CreateProductAsync(CreateProductDto createProductDto)
    {
        try
        {
            var product = _mapper.Map<Product>(createProductDto);
            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;

            var createdProduct = await _productRepository.AddAsync(product);
            await _productRepository.SaveChangesAsync();

            _logger.LogInformation("Product created with ID {ProductId}", createdProduct.Id);
            return _mapper.Map<ProductDto>(createdProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product {ProductName}", createProductDto.Name);
            throw;
        }
    }

    public async Task<ProductDto?> UpdateProductAsync(int id, UpdateProductDto updateProductDto)
    {
        try
        {
            var existingProduct = await _productRepository.GetByIdAsync(id);
            if (existingProduct == null)
                throw new ResourceNotFoundException($"Product with ID {id} not found");

            // Update only non-null properties
            if (!string.IsNullOrEmpty(updateProductDto.Name))
                existingProduct.Name = updateProductDto.Name;
            
            if (!string.IsNullOrEmpty(updateProductDto.Description))
                existingProduct.Description = updateProductDto.Description;
            
            if (updateProductDto.Price.HasValue)
                existingProduct.Price = updateProductDto.Price.Value;
            
            if (updateProductDto.StockQuantity.HasValue)
                existingProduct.StockQuantity = updateProductDto.StockQuantity.Value;
            
            if (!string.IsNullOrEmpty(updateProductDto.ImageUrl))
                existingProduct.ImageUrl = updateProductDto.ImageUrl;
            
            if (updateProductDto.IsActive.HasValue)
                existingProduct.IsActive = updateProductDto.IsActive.Value;

            existingProduct.UpdatedAt = DateTime.UtcNow;

            _productRepository.Update(existingProduct);
            await _productRepository.SaveChangesAsync();

            _logger.LogInformation("Product updated with ID {ProductId}", id);
            return _mapper.Map<ProductDto>(existingProduct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID {ProductId}", id);
            throw;
        }
    }

    public async Task<bool> UpdateProductStockAsync(int id, UpdateProductStockDto updateStockDto)
    {
        try
        {
            var success = await _productRepository.UpdateStockAsync(id, updateStockDto.Quantity);
            if (success)
            {
                _logger.LogInformation("Stock updated for product ID {ProductId} to {Quantity}", id, updateStockDto.Quantity);
            }
            return success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for product ID {ProductId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteProductAsync(int id)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                return false;

            _productRepository.Remove(product);
            await _productRepository.SaveChangesAsync();

            _logger.LogInformation("Product deleted with ID {ProductId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID {ProductId}", id);
            throw;
        }
    }

    public async Task<bool> ActivateProductAsync(int id)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                return false;

            product.IsActive = true;
            product.UpdatedAt = DateTime.UtcNow;
            
            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync();

            _logger.LogInformation("Product activated with ID {ProductId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating product with ID {ProductId}", id);
            throw;
        }
    }

    public async Task<bool> DeactivateProductAsync(int id)
    {
        try
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                return false;

            product.IsActive = false;
            product.UpdatedAt = DateTime.UtcNow;
            
            _productRepository.Update(product);
            await _productRepository.SaveChangesAsync();

            _logger.LogInformation("Product deactivated with ID {ProductId}", id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating product with ID {ProductId}", id);
            throw;
        }
    }

    public async Task<(IEnumerable<ProductDto> Products, int TotalCount)> GetPagedProductsAsync(int page, int pageSize, string? searchTerm = null)
    {
        try
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;
            if (pageSize > 100) pageSize = 100; // Limit page size

            var products = string.IsNullOrEmpty(searchTerm) 
                ? await _productRepository.GetAllAsync()
                : await _productRepository.SearchProductsAsync(searchTerm);

            var totalCount = products.Count();
            var pagedProducts = products
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var productDtos = _mapper.Map<IEnumerable<ProductDto>>(pagedProducts);
            
            return (productDtos, totalCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving paged products (page: {Page}, size: {PageSize})", page, pageSize);
            throw;
        }
    }
}
