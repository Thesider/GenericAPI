using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using GenericAPI.Services;
using GenericAPI.DTOs;

namespace GenericAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly ILogger<ProductController> _logger;

    public ProductController(IProductService productService, ILogger<ProductController> logger)
    {
        _productService = productService;
        _logger = logger;
    }

    /// <summary>
    /// Get all products with optional pagination and search
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null)
    {
        try
        {
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "Invalid pagination parameters" });
            }            var (products, totalCount) = await _productService.GetPagedProductsAsync(page, pageSize, search);
            
            Response.Headers["X-Total-Count"] = totalCount.ToString();
            Response.Headers["X-Page"] = page.ToString();
            Response.Headers["X-Page-Size"] = pageSize.ToString();

            return Ok(new
            {
                data = products,
                pagination = new
                {
                    currentPage = page,
                    pageSize,
                    totalCount,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products");
            return StatusCode(500, new { message = "An error occurred while retrieving products" });
        }
    }

    /// <summary>
    /// Get all products (simple list without pagination)
    /// </summary>
    [HttpGet("all")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetAllProducts()
    {
        try
        {
            var products = await _productService.GetAllProductsAsync();
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all products");
            return StatusCode(500, new { message = "An error occurred while retrieving products" });
        }
    }

    /// <summary>
    /// Get a specific product by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(int id)
    {
        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
                return NotFound(new { message = $"Product with ID {id} not found" });

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID {ProductId}", id);
            return StatusCode(500, new { message = "An error occurred while retrieving the product" });
        }
    }

    /// <summary>
    /// Search products by name or description
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> SearchProducts([FromQuery] string term)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(term))
                return BadRequest(new { message = "Search term is required" });

            var products = await _productService.SearchProductsAsync(term.Trim());
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching products with term: {SearchTerm}", term);
            return StatusCode(500, new { message = "An error occurred while searching products" });
        }
    }

    /// <summary>
    /// Get products by price range
    /// </summary>
    [HttpGet("price-range")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByPriceRange(
        [FromQuery] decimal minPrice,
        [FromQuery] decimal maxPrice)
    {
        try
        {
            var products = await _productService.GetProductsByPriceRangeAsync(minPrice, maxPrice);
            return Ok(products);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products by price range {MinPrice}-{MaxPrice}", minPrice, maxPrice);
            return StatusCode(500, new { message = "An error occurred while retrieving products" });
        }
    }

    /// <summary>
    /// Get products that are currently in stock
    /// </summary>
    [HttpGet("in-stock")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsInStock()
    {
        try
        {
            var products = await _productService.GetProductsInStockAsync();
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products in stock");
            return StatusCode(500, new { message = "An error occurred while retrieving products in stock" });
        }
    }

    /// <summary>
    /// Get products with low stock (below threshold)
    /// </summary>
    [HttpGet("low-stock")]
    public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsWithLowStock([FromQuery] int threshold = 10)
    {
        try
        {
            var products = await _productService.GetProductsWithLowStockAsync(threshold);
            return Ok(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products with low stock");
            return StatusCode(500, new { message = "An error occurred while retrieving products with low stock" });
        }
    }

    /// <summary>
    /// Create a new product (Admin only)
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto createProductDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new 
                { 
                    message = "Validation failed", 
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var product = await _productService.CreateProductAsync(createProductDto);
            return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product");
            return StatusCode(500, new { message = "An error occurred while creating the product" });
        }
    }

    /// <summary>
    /// Update an existing product (Admin only)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult<ProductDto>> UpdateProduct(int id, UpdateProductDto updateProductDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new 
                { 
                    message = "Validation failed", 
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var product = await _productService.UpdateProductAsync(id, updateProductDto);
            if (product == null)
                return NotFound(new { message = $"Product with ID {id} not found" });

            return Ok(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID {ProductId}", id);
            return StatusCode(500, new { message = "An error occurred while updating the product" });
        }
    }

    /// <summary>
    /// Update product stock quantity (Admin only)
    /// </summary>
    [HttpPatch("{id}/stock")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> UpdateProductStock(int id, UpdateProductStockDto updateStockDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new 
                { 
                    message = "Validation failed", 
                    errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                });
            }

            var success = await _productService.UpdateProductStockAsync(id, updateStockDto);
            if (!success)
                return NotFound(new { message = $"Product with ID {id} not found" });

            return Ok(new { message = "Stock updated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating stock for product with ID {ProductId}", id);
            return StatusCode(500, new { message = "An error occurred while updating product stock" });
        }
    }

    /// <summary>
    /// Activate a product (Admin only)
    /// </summary>
    [HttpPatch("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> ActivateProduct(int id)
    {
        try
        {
            var success = await _productService.ActivateProductAsync(id);
            if (!success)
                return NotFound(new { message = $"Product with ID {id} not found" });

            return Ok(new { message = "Product activated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error activating product with ID {ProductId}", id);
            return StatusCode(500, new { message = "An error occurred while activating the product" });
        }
    }

    /// <summary>
    /// Deactivate a product (Admin only)
    /// </summary>
    [HttpPatch("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeactivateProduct(int id)
    {
        try
        {
            var success = await _productService.DeactivateProductAsync(id);
            if (!success)
                return NotFound(new { message = $"Product with ID {id} not found" });

            return Ok(new { message = "Product deactivated successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating product with ID {ProductId}", id);
            return StatusCode(500, new { message = "An error occurred while deactivating the product" });
        }
    }

    /// <summary>
    /// Delete a product (Admin only)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> DeleteProduct(int id)
    {
        try
        {
            var success = await _productService.DeleteProductAsync(id);
            if (!success)
                return NotFound(new { message = $"Product with ID {id} not found" });

            return Ok(new { message = "Product deleted successfully" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID {ProductId}", id);
            return StatusCode(500, new { message = "An error occurred while deleting the product" });
        }
    }
}
