using Microsoft.EntityFrameworkCore;
using ContosoInventory.Server.Models;
using ContosoInventory.Server.Repositories;
using ContosoInventory.Shared.DTOs;

namespace ContosoInventory.Server.Services;

/// <summary>
/// Implements product validation, business rules, and entity-to-DTO mapping.
/// </summary>
public sealed class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;
    private readonly ILogger<ProductService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ProductService"/> class.
    /// </summary>
    public ProductService(IProductRepository productRepository, ILogger<ProductService> logger)
    {
        _productRepository = productRepository;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<ProductResponseDto>> GetAllProductsAsync(
        int? categoryId,
        CancellationToken cancellationToken)
    {
        if (categoryId is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(categoryId), "Category ID must be positive.");
        }

        try
        {
            var products = await _productRepository.GetAllAsync(categoryId, cancellationToken);
            return products.Select(MapToDto).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving products for category {CategoryId}.", categoryId);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> GetProductByIdAsync(int id, CancellationToken cancellationToken)
    {
        ValidateId(id);

        try
        {
            var product = await _productRepository.GetByIdAsync(id, trackChanges: false, cancellationToken);
            return product is null ? null : MapToDto(product);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving product with ID {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto> CreateProductAsync(
        CreateProductDto dto,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dto);
        ValidateFields(dto.Name, dto.Sku, dto.Description, dto.Price, dto.StockQuantity, dto.CategoryId);

        var sku = dto.Sku.Trim();
        var name = dto.Name.Trim();
        var description = dto.Description.Trim();

        try
        {
            if (!await _productRepository.CategoryExistsAsync(dto.CategoryId, cancellationToken))
            {
                throw new ArgumentException($"Category {dto.CategoryId} does not exist.", nameof(dto.CategoryId));
            }

            if (await _productRepository.SkuExistsAsync(sku, excludedProductId: null, cancellationToken))
            {
                throw new ProductSkuConflictException(sku);
            }

            var now = DateTime.UtcNow;
            var product = new Product
            {
                Name = name,
                Sku = sku,
                Description = description,
                Price = dto.Price,
                StockQuantity = dto.StockQuantity,
                CategoryId = dto.CategoryId,
                CreatedDate = now,
                LastUpdatedDate = now
            };

            await _productRepository.AddAsync(product, cancellationToken);
            _logger.LogInformation("Product created: {ProductName} (ID: {ProductId}).", product.Name, product.Id);
            return MapToDto(product);
        }
        catch (ProductSkuConflictException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (DbUpdateException ex)
        {
            if (await IsSkuConflictAsync(ex, sku, null, "creating", cancellationToken))
            {
                throw new ProductSkuConflictException(sku);
            }

            _logger.LogError(ex, "Error creating product with SKU {ProductSku}.", sku);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating product with SKU {ProductSku}.", sku);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> UpdateProductAsync(
        int id,
        UpdateProductDto dto,
        CancellationToken cancellationToken)
    {
        ValidateId(id);
        ArgumentNullException.ThrowIfNull(dto);
        ValidateFields(dto.Name, dto.Sku, dto.Description, dto.Price, dto.StockQuantity, dto.CategoryId);

        var sku = dto.Sku.Trim();
        try
        {
            var product = await _productRepository.GetByIdAsync(id, trackChanges: true, cancellationToken);
            if (product is null)
            {
                return null;
            }

            if (!await _productRepository.CategoryExistsAsync(dto.CategoryId, cancellationToken))
            {
                throw new ArgumentException($"Category {dto.CategoryId} does not exist.", nameof(dto.CategoryId));
            }

            if (await _productRepository.SkuExistsAsync(sku, id, cancellationToken))
            {
                throw new ProductSkuConflictException(sku);
            }

            product.Name = dto.Name.Trim();
            product.Sku = sku;
            product.Description = dto.Description.Trim();
            product.Price = dto.Price;
            product.StockQuantity = dto.StockQuantity;
            product.CategoryId = dto.CategoryId;
            product.LastUpdatedDate = DateTime.UtcNow;

            await _productRepository.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Product updated: {ProductName} (ID: {ProductId}).", product.Name, product.Id);
            return MapToDto(product);
        }
        catch (ProductSkuConflictException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            throw;
        }
        catch (DbUpdateException ex)
        {
            if (await IsSkuConflictAsync(ex, sku, id, "updating", cancellationToken))
            {
                throw new ProductSkuConflictException(sku);
            }

            _logger.LogError(ex, "Error updating product with ID {ProductId}.", id);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating product with ID {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<bool> DeleteProductAsync(int id, CancellationToken cancellationToken)
    {
        ValidateId(id);

        try
        {
            var product = await _productRepository.GetByIdAsync(id, trackChanges: true, cancellationToken);
            if (product is null)
            {
                return false;
            }

            await _productRepository.DeleteAsync(product, cancellationToken);
            _logger.LogInformation("Product deleted: {ProductName} (ID: {ProductId}).", product.Name, product.Id);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting product with ID {ProductId}.", id);
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<ProductResponseDto?> RestockProductAsync(
        int id,
        RestockProductDto dto,
        CancellationToken cancellationToken)
    {
        ValidateId(id);
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(dto.Quantity), "Restock quantity must be positive.");
        }

        try
        {
            var product = await _productRepository.GetByIdAsync(id, trackChanges: true, cancellationToken);
            if (product is null)
            {
                return null;
            }

            if (dto.Quantity > int.MaxValue - product.StockQuantity)
            {
                throw new ArgumentOutOfRangeException(nameof(dto.Quantity), "Restock quantity exceeds the stock limit.");
            }

            product.StockQuantity += dto.Quantity;
            product.LastUpdatedDate = DateTime.UtcNow;
            await _productRepository.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Product restocked: {ProductId}, added {Quantity}, total {StockQuantity}.",
                product.Id, dto.Quantity, product.StockQuantity);
            return MapToDto(product);
        }
        catch (ArgumentOutOfRangeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restocking product with ID {ProductId}.", id);
            throw;
        }
    }

    private static void ValidateId(int id)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "Product ID must be positive.");
        }
    }

    private async Task<bool> IsSkuConflictAsync(
        DbUpdateException saveException,
        string sku,
        int? excludedProductId,
        string operation,
        CancellationToken cancellationToken)
    {
        try
        {
            if (!await _productRepository.SkuExistsAsync(sku, excludedProductId, cancellationToken))
            {
                return false;
            }

            _logger.LogWarning(
                saveException,
                "SKU {ProductSku} became a conflict while {Operation} a product.",
                sku,
                operation);
            return true;
        }
        catch (Exception checkException)
        {
            _logger.LogError(
                checkException,
                "Unable to verify whether SKU {ProductSku} conflicted while {Operation} a product.",
                sku,
                operation);
            throw;
        }
    }

    private static void ValidateFields(
        string name,
        string sku,
        string description,
        decimal price,
        int stockQuantity,
        int categoryId)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length is < 2 or > 200)
        {
            throw new ArgumentException("Product name must contain 2 to 200 characters.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(sku) || sku.Trim().Length > 64)
        {
            throw new ArgumentException("Product SKU must contain 1 to 64 characters.", nameof(sku));
        }

        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 1000)
        {
            throw new ArgumentException("Product description must contain 1 to 1000 characters.", nameof(description));
        }

        if (price is < 0.01m or > 999999.99m)
        {
            throw new ArgumentOutOfRangeException(nameof(price), "Price must be between 0.01 and 999999.99.");
        }

        if (stockQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(stockQuantity), "Stock quantity cannot be negative.");
        }

        if (categoryId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(categoryId), "Category ID must be positive.");
        }
    }

    private static ProductResponseDto MapToDto(Product product)
    {
        return new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            Sku = product.Sku,
            Description = product.Description,
            Price = product.Price,
            StockQuantity = product.StockQuantity,
            CategoryId = product.CategoryId,
            CreatedDate = product.CreatedDate,
            LastUpdatedDate = product.LastUpdatedDate
        };
    }
}
