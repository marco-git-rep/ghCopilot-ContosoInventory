using ContosoInventory.Server.Models;

namespace ContosoInventory.Server.Repositories;

/// <summary>
/// Defines persistence operations for categories.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Retrieves all categories ordered by display order.
    /// </summary>
    Task<List<Category>> GetAllAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a category by ID, optionally tracking changes.
    /// </summary>
    Task<Category?> GetByIdAsync(int id, bool trackChanges, CancellationToken cancellationToken);

    /// <summary>
    /// Determines whether another category has the same name.
    /// </summary>
    Task<bool> NameExistsAsync(string name, int? excludedCategoryId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds and persists a new category.
    /// </summary>
    Task AddAsync(Category category, CancellationToken cancellationToken);

    /// <summary>
    /// Persists changes to a tracked category.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Deletes and persists a category.
    /// </summary>
    Task DeleteAsync(Category category, CancellationToken cancellationToken);
}
