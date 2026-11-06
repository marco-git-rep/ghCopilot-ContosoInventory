using ContosoInventory.Server.Models;

namespace ContosoInventory.Server.Repositories;

/// <summary>
/// Defines persistence operations for products.
/// </summary>
public interface IProductRepository
{
    /// <summary>
    /// Retrieves products, optionally filtered by category.
    /// </summary>
    Task<List<Product>> GetAllAsync(int? categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a product by ID.
    /// </summary>
    Task<Product?> GetByIdAsync(int id, bool trackChanges, CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether a category exists.
    /// </summary>
    Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether any products reference a category.
    /// </summary>
    Task<bool> HasProductsInCategoryAsync(int categoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether an SKU is already assigned, optionally excluding a product.
    /// </summary>
    Task<bool> SkuExistsAsync(string sku, int? excludedProductId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds and persists a new product.
    /// </summary>
    Task AddAsync(Product product, CancellationToken cancellationToken);

    /// <summary>
    /// Persists changes to a tracked product.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Deletes and persists a product.
    /// </summary>
    Task DeleteAsync(Product product, CancellationToken cancellationToken);
}
