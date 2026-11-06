using Microsoft.EntityFrameworkCore;
using ContosoInventory.Server.Data;
using ContosoInventory.Server.Models;

namespace ContosoInventory.Server.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence operations for products.
/// </summary>
public sealed class ProductRepository : IProductRepository
{
    private readonly InventoryContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductRepository"/> class.
    /// </summary>
    /// <param name="context">The inventory database context.</param>
    public ProductRepository(InventoryContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<List<Product>> GetAllAsync(int? categoryId, CancellationToken cancellationToken)
    {
        var products = _context.Products.AsNoTracking();
        if (categoryId.HasValue)
        {
            products = products.Where(product => product.CategoryId == categoryId.Value);
        }

        return await products.OrderBy(product => product.Id).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Product?> GetByIdAsync(int id, bool trackChanges, CancellationToken cancellationToken)
    {
        var products = _context.Products.AsQueryable();
        if (!trackChanges)
        {
            products = products.AsNoTracking();
        }

        return await products.FirstOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> CategoryExistsAsync(int categoryId, CancellationToken cancellationToken)
    {
        return _context.Categories.AnyAsync(category => category.Id == categoryId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> HasProductsInCategoryAsync(int categoryId, CancellationToken cancellationToken)
    {
        return _context.Products.AnyAsync(product => product.CategoryId == categoryId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> SkuExistsAsync(string sku, int? excludedProductId, CancellationToken cancellationToken)
    {
        return _context.Products.AnyAsync(
            product => product.Sku == sku &&
                (!excludedProductId.HasValue || product.Id != excludedProductId.Value),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Product product, CancellationToken cancellationToken)
    {
        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Product product, CancellationToken cancellationToken)
    {
        _context.Products.Remove(product);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
