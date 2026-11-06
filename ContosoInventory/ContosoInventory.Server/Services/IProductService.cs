using ContosoInventory.Shared.DTOs;

namespace ContosoInventory.Server.Services;

/// <summary>
/// Defines business operations for inventory products.
/// </summary>
public interface IProductService
{
    /// <summary>
    /// Retrieves all products, optionally filtered by category.
    /// </summary>
    Task<List<ProductResponseDto>> GetAllProductsAsync(int? categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a product by its identifier.
    /// </summary>
    Task<ProductResponseDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Creates a product after validating its fields, category, and SKU.
    /// </summary>
    Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Updates a product, or returns null when it does not exist.
    /// </summary>
    Task<ProductResponseDto?> UpdateProductAsync(int id, UpdateProductDto dto, CancellationToken cancellationToken);

    /// <summary>
    /// Deletes a product, returning false when it does not exist.
    /// </summary>
    Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken);

    /// <summary>
    /// Adds the requested quantity to a product's stock.
    /// </summary>
    Task<ProductResponseDto?> RestockProductAsync(int id, RestockProductDto dto, CancellationToken cancellationToken);
}
