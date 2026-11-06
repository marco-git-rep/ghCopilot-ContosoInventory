namespace ContosoInventory.Server.Services;

/// <summary>
/// Indicates that a product SKU is already assigned to another product.
/// </summary>
public sealed class ProductSkuConflictException : InvalidOperationException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ProductSkuConflictException"/> class.
    /// </summary>
    /// <param name="sku">The SKU that is already in use.</param>
    public ProductSkuConflictException(string sku)
        : base($"A product with the SKU '{sku}' already exists.")
    {
    }
}
