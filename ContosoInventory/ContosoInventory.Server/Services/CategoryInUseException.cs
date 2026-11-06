namespace ContosoInventory.Server.Services;

/// <summary>
/// Indicates that a category cannot be deleted while products reference it.
/// </summary>
public sealed class CategoryInUseException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CategoryInUseException"/> class.
    /// </summary>
    /// <param name="categoryId">The category that is still in use.</param>
    public CategoryInUseException(int categoryId)
        : base($"Category {categoryId} cannot be deleted because it contains products.")
    {
    }
}
