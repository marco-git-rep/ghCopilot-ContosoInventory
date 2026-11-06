namespace ContosoInventory.Shared.DTOs;

/// <summary>
/// Provides the editable fields for updating a product.
/// </summary>
public class UpdateProductDto
{
    /// <summary>
    /// Gets or sets the product name.
    /// </summary>
    [Required]
    [StringLength(200, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the stock keeping unit.
    /// </summary>
    [Required]
    [StringLength(64, MinimumLength = 1)]
    public string Sku { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the product description.
    /// </summary>
    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the unit price.
    /// </summary>
    [Range(typeof(decimal), "0.01", "999999.99")]
    public decimal Price { get; set; }

    /// <summary>
    /// Gets or sets the available stock quantity.
    /// </summary>
    [Range(0, int.MaxValue)]
    public int StockQuantity { get; set; }

    /// <summary>
    /// Gets or sets the category identifier.
    /// </summary>
    [Range(1, int.MaxValue)]
    public int CategoryId { get; set; }
}
