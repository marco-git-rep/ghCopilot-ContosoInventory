using Microsoft.EntityFrameworkCore;
using ContosoInventory.Server.Data;
using ContosoInventory.Server.Models;

namespace ContosoInventory.Server.Repositories;

/// <summary>
/// Provides Entity Framework Core persistence operations for categories.
/// </summary>
public sealed class CategoryRepository : ICategoryRepository
{
    private readonly InventoryContext _context;

    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryRepository"/> class.
    /// </summary>
    /// <param name="context">The inventory database context.</param>
    public CategoryRepository(InventoryContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public Task<List<Category>> GetAllAsync(CancellationToken cancellationToken)
    {
        return _context.Categories
            .AsNoTracking()
            .OrderBy(category => category.DisplayOrder)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Category?> GetByIdAsync(int id, bool trackChanges, CancellationToken cancellationToken)
    {
        var categories = _context.Categories.AsQueryable();
        if (!trackChanges)
        {
            categories = categories.AsNoTracking();
        }

        return await categories.FirstOrDefaultAsync(category => category.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<bool> NameExistsAsync(
        string name,
        int? excludedCategoryId,
        CancellationToken cancellationToken)
    {
        var normalizedName = name.ToLower();
        return _context.Categories.AnyAsync(
            category => category.Name.ToLower() == normalizedName &&
                (!excludedCategoryId.HasValue || category.Id != excludedCategoryId.Value),
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task AddAsync(Category category, CancellationToken cancellationToken)
    {
        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(Category category, CancellationToken cancellationToken)
    {
        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
