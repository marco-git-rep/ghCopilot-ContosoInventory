namespace ContosoInventory.Shared.DTOs;

/// <summary>
/// Provides the quantity to add when restocking a product.
/// </summary>
public class RestockProductDto
{
    /// <summary>
    /// Gets or sets the positive quantity to add to existing stock.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int Quantity { get; set; }
}
