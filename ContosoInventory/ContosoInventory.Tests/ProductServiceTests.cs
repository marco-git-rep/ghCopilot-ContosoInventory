using System.ComponentModel.DataAnnotations;
using System.Reflection;
using ContosoInventory.Server.Controllers;
using ContosoInventory.Server.Data;
using ContosoInventory.Server.Models;
using ContosoInventory.Server.Repositories;
using ContosoInventory.Server.Services;
using ContosoInventory.Shared.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ContosoInventory.Tests;

public sealed class ProductServiceTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private InventoryContext _context = null!;
    private ProductService _service = null!;

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InventoryContext>()
            .UseSqlite(_connection)
            .Options;
        _context = new InventoryContext(options);
        await _context.Database.EnsureCreatedAsync();

        _context.Categories.AddRange(
            CreateCategory(1, "Peripherals"),
            CreateCategory(2, "Monitors"));
        await _context.SaveChangesAsync();

        _service = new ProductService(
            new ProductRepository(_context),
            Mock.Of<ILogger<ProductService>>());
    }

    public async Task DisposeAsync()
    {
        await _context.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public async Task CreateProductAsync_ValidProduct_PersistsProductAndUtcDates()
    {
        var created = await _service.CreateProductAsync(CreateDto(), CancellationToken.None);

        Assert.True(created.Id > 0);
        Assert.Equal("Wireless Mouse", created.Name);
        Assert.Equal("MOUSE-100", created.Sku);
        Assert.Equal("Wireless ergonomic mouse", created.Description);
        Assert.Equal(29.99m, created.Price);
        Assert.Equal(25, created.StockQuantity);
        Assert.Equal(1, created.CategoryId);
        Assert.Equal(DateTimeKind.Utc, created.CreatedDate.Kind);
        Assert.Equal(created.CreatedDate, created.LastUpdatedDate);
        Assert.Equal(1, await _context.Products.CountAsync());
    }

    [Fact]
    public async Task CreateProductAsync_PriceAtBothValidBounds_PersistsProducts()
    {
        var minimumPrice = await _service.CreateProductAsync(
            CreateDto(sku: "MIN-PRICE", price: 0.01m),
            CancellationToken.None);
        var maximumPrice = await _service.CreateProductAsync(
            CreateDto(sku: "MAX-PRICE", price: 999999.99m),
            CancellationToken.None);

        Assert.Equal(0.01m, minimumPrice.Price);
        Assert.Equal(999999.99m, maximumPrice.Price);
        Assert.Equal(2, await _context.Products.CountAsync());
    }

    [Fact]
    public async Task CreateProductAsync_DuplicateSkuIgnoringCase_ThrowsConflict()
    {
        await _service.CreateProductAsync(CreateDto(sku: "ABC-123"), CancellationToken.None);

        await Assert.ThrowsAsync<ProductSkuConflictException>(
            () => _service.CreateProductAsync(CreateDto(sku: "abc-123"), CancellationToken.None));
    }

    [Fact]
    public async Task CreateProductAsync_UnknownCategory_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.CreateProductAsync(CreateDto(categoryId: 99), CancellationToken.None));
    }

    [Fact]
    public async Task CreateProductAsync_InvalidPrice_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.CreateProductAsync(CreateDto(price: 0), CancellationToken.None));
    }

    [Fact]
    public async Task GetAllProductsAsync_CategoryFilter_ReturnsOnlyMatchingProducts()
    {
        await _service.CreateProductAsync(CreateDto(sku: "MOUSE-100", categoryId: 1), CancellationToken.None);
        await _service.CreateProductAsync(CreateDto(sku: "MONITOR-100", categoryId: 2), CancellationToken.None);

        var products = await _service.GetAllProductsAsync(1, CancellationToken.None);

        var product = Assert.Single(products);
        Assert.Equal(1, product.CategoryId);
        Assert.Equal("MOUSE-100", product.Sku);
    }

    [Fact]
    public async Task GetAllProductsAsync_NoCategoryFilter_ReturnsAllProducts()
    {
        await _service.CreateProductAsync(CreateDto(sku: "MOUSE-100", categoryId: 1), CancellationToken.None);
        await _service.CreateProductAsync(CreateDto(sku: "MONITOR-100", categoryId: 2), CancellationToken.None);

        var products = await _service.GetAllProductsAsync(null, CancellationToken.None);

        Assert.Equal(2, products.Count);
        Assert.Equal(new[] { "MOUSE-100", "MONITOR-100" }, products.Select(product => product.Sku));
    }

    [Fact]
    public async Task GetProductByIdAsync_ExistingProduct_ReturnsProductDetails()
    {
        var created = await _service.CreateProductAsync(CreateDto(), CancellationToken.None);

        var product = await _service.GetProductByIdAsync(created.Id, CancellationToken.None);

        Assert.NotNull(product);
        Assert.Equal(created.Id, product.Id);
        Assert.Equal("Wireless Mouse", product.Name);
        Assert.Equal("MOUSE-100", product.Sku);
        Assert.Equal(29.99m, product.Price);
        Assert.Equal(25, product.StockQuantity);
        Assert.Equal(1, product.CategoryId);
    }

    [Fact]
    public async Task GetProductByIdAsync_MissingProduct_ReturnsNull()
    {
        var product = await _service.GetProductByIdAsync(42, CancellationToken.None);

        Assert.Null(product);
    }

    [Fact]
    public async Task UpdateProductAsync_ValidUpdate_PersistsChangesAndPreservesCreatedDate()
    {
        var created = await _service.CreateProductAsync(CreateDto(), CancellationToken.None);
        var originalCreatedDate = created.CreatedDate;

        var updated = await _service.UpdateProductAsync(
            created.Id,
            new UpdateProductDto
            {
                Name = "Updated mouse",
                Sku = "MOUSE-200",
                Description = "An updated mouse",
                Price = 35.99m,
                StockQuantity = 8,
                CategoryId = 2
            },
            CancellationToken.None);

        Assert.NotNull(updated);
        Assert.Equal("Updated mouse", updated.Name);
        Assert.Equal(2, updated.CategoryId);
        Assert.Equal(originalCreatedDate, updated.CreatedDate);
        Assert.True(updated.LastUpdatedDate >= originalCreatedDate);
    }

    [Fact]
    public async Task UpdateProductAsync_DuplicateSkuIgnoringCase_ThrowsConflict()
    {
        await _service.CreateProductAsync(CreateDto(sku: "ABC-123"), CancellationToken.None);
        var second = await _service.CreateProductAsync(CreateDto(sku: "XYZ-123"), CancellationToken.None);

        await Assert.ThrowsAsync<ProductSkuConflictException>(() =>
            _service.UpdateProductAsync(
                second.Id,
                new UpdateProductDto
                {
                    Name = "Updated mouse",
                    Sku = "abc-123",
                    Description = "An updated mouse",
                    Price = 35.99m,
                    StockQuantity = 8,
                    CategoryId = 1
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task UpdateProductAsync_InvalidStock_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.UpdateProductAsync(
                1,
                new UpdateProductDto
                {
                    Name = "Updated mouse",
                    Sku = "MOUSE-200",
                    Description = "An updated mouse",
                    Price = 35.99m,
                    StockQuantity = -1,
                    CategoryId = 1
                },
                CancellationToken.None));
    }

    [Fact]
    public async Task UpdateProductAsync_MissingProduct_ReturnsNull()
    {
        var updated = await _service.UpdateProductAsync(
            42,
            new UpdateProductDto
            {
                Name = "Updated mouse",
                Sku = "MOUSE-200",
                Description = "An updated mouse",
                Price = 35.99m,
                StockQuantity = 8,
                CategoryId = 1
            },
            CancellationToken.None);

        Assert.Null(updated);
    }

    [Fact]
    public async Task DeleteProductAsync_ExistingProduct_RemovesProduct()
    {
        var product = await _service.CreateProductAsync(CreateDto(), CancellationToken.None);

        var deleted = await _service.DeleteProductAsync(product.Id, CancellationToken.None);

        Assert.True(deleted);
        Assert.False(await _context.Products.AnyAsync());
    }

    [Fact]
    public async Task DeleteProductAsync_MissingProduct_ReturnsFalse()
    {
        var deleted = await _service.DeleteProductAsync(42, CancellationToken.None);

        Assert.False(deleted);
    }

    [Fact]
    public async Task RestockProductAsync_PositiveQuantity_IncreasesStockAndUpdatesDate()
    {
        var created = await _service.CreateProductAsync(CreateDto(stockQuantity: 5), CancellationToken.None);

        var restocked = await _service.RestockProductAsync(
            created.Id,
            new RestockProductDto { Quantity = 7 },
            CancellationToken.None);

        Assert.NotNull(restocked);
        Assert.Equal(12, restocked.StockQuantity);
        Assert.True(restocked.LastUpdatedDate >= created.LastUpdatedDate);
    }

    [Fact]
    public async Task RestockProductAsync_ZeroQuantity_ThrowsArgumentOutOfRangeException()
    {
        var created = await _service.CreateProductAsync(CreateDto(), CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.RestockProductAsync(
                created.Id,
                new RestockProductDto { Quantity = 0 },
                CancellationToken.None));
    }

    [Fact]
    public async Task RestockProductAsync_ExceedsStockLimit_ThrowsArgumentOutOfRangeException()
    {
        var created = await _service.CreateProductAsync(
            CreateDto(stockQuantity: int.MaxValue - 1),
            CancellationToken.None);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _service.RestockProductAsync(
                created.Id,
                new RestockProductDto { Quantity = 2 },
                CancellationToken.None));
    }

    [Fact]
    public async Task RestockProductAsync_MissingProduct_ReturnsNull()
    {
        var product = await _service.RestockProductAsync(
            42,
            new RestockProductDto { Quantity = 1 },
            CancellationToken.None);

        Assert.Null(product);
    }

    [Fact]
    public void CreateProductDto_InvalidFields_FailsDataAnnotationValidation()
    {
        var dto = CreateDto(name: "x", sku: "", description: new string('d', 1001), price: 1000000m, categoryId: 0);
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        var invalidMembers = validationResults.SelectMany(result => result.MemberNames).ToHashSet();
        Assert.Contains(nameof(dto.Name), invalidMembers);
        Assert.Contains(nameof(dto.Sku), invalidMembers);
        Assert.Contains(nameof(dto.Description), invalidMembers);
        Assert.Contains(nameof(dto.Price), invalidMembers);
        Assert.Contains(nameof(dto.CategoryId), invalidMembers);
    }

    [Fact]
    public void UpdateProductDto_InvalidFields_FailsDataAnnotationValidation()
    {
        var dto = new UpdateProductDto
        {
            Name = "x",
            Sku = "",
            Description = new string('d', 1001),
            Price = 1000000m,
            StockQuantity = -1,
            CategoryId = 0
        };
        var validationResults = new List<ValidationResult>();
        var isValid = Validator.TryValidateObject(
            dto,
            new ValidationContext(dto),
            validationResults,
            validateAllProperties: true);

        Assert.False(isValid);
        var invalidMembers = validationResults.SelectMany(result => result.MemberNames).ToHashSet();
        Assert.Contains(nameof(dto.Name), invalidMembers);
        Assert.Contains(nameof(dto.Sku), invalidMembers);
        Assert.Contains(nameof(dto.Description), invalidMembers);
        Assert.Contains(nameof(dto.Price), invalidMembers);
        Assert.Contains(nameof(dto.StockQuantity), invalidMembers);
        Assert.Contains(nameof(dto.CategoryId), invalidMembers);
    }

    [Fact]
    public async Task DeleteCategoryAsync_ProductsReferenceCategory_ThrowsConflict()
    {
        await _service.CreateProductAsync(CreateDto(categoryId: 1), CancellationToken.None);
        var categoryService = new CategoryService(
            new CategoryRepository(_context),
            new ProductRepository(_context),
            Mock.Of<ILogger<CategoryService>>());

        await Assert.ThrowsAsync<CategoryInUseException>(
            () => categoryService.DeleteCategoryAsync(1, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteCategoryAsync_NoProductsReferenceCategory_DeletesCategory()
    {
        var categoryService = new CategoryService(
            new CategoryRepository(_context),
            new ProductRepository(_context),
            Mock.Of<ILogger<CategoryService>>());

        var deleted = await categoryService.DeleteCategoryAsync(1, CancellationToken.None);

        Assert.True(deleted);
        Assert.False(await _context.Categories.AnyAsync(category => category.Id == 1));
    }

    [Fact]
    public async Task DeleteCategoryAsync_ForwardsCancellationTokenToRepositories()
    {
        using var cancellationSource = new CancellationTokenSource();
        var cancellationToken = cancellationSource.Token;
        var categoryRepository = new Mock<ICategoryRepository>();
        categoryRepository
            .Setup(repository => repository.GetByIdAsync(1, true, cancellationToken))
            .ReturnsAsync(CreateCategory(1, "Peripherals"));
        var productRepository = new Mock<IProductRepository>();
        productRepository
            .Setup(repository => repository.HasProductsInCategoryAsync(1, cancellationToken))
            .ReturnsAsync(true);
        var categoryService = new CategoryService(
            categoryRepository.Object,
            productRepository.Object,
            Mock.Of<ILogger<CategoryService>>());

        await Assert.ThrowsAsync<CategoryInUseException>(
            () => categoryService.DeleteCategoryAsync(1, cancellationToken));

        categoryRepository.Verify(
            repository => repository.GetByIdAsync(1, true, cancellationToken),
            Times.Once);
        productRepository.Verify(
            repository => repository.HasProductsInCategoryAsync(1, cancellationToken),
            Times.Once);
    }

    [Fact]
    public void ProductsController_ReadActionsRequireAuthenticationAndWriteActionsRequireAdmin()
    {
        var controllerAuthorization = typeof(ProductsController).GetCustomAttribute<AuthorizeAttribute>();
        Assert.NotNull(controllerAuthorization);
        Assert.Null(controllerAuthorization.Roles);

        foreach (var actionName in new[] { "CreateProduct", "UpdateProduct", "DeleteProduct", "RestockProduct" })
        {
            var action = typeof(ProductsController).GetMethod(actionName);
            Assert.NotNull(action);
            var authorization = action.GetCustomAttribute<AuthorizeAttribute>();
            Assert.NotNull(authorization);
            Assert.Equal("Admin", authorization.Roles);
        }
    }

    private static Category CreateCategory(int id, string name)
    {
        return new Category
        {
            Id = id,
            Name = name,
            Description = $"{name} category",
            DisplayOrder = id,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };
    }

    private static CreateProductDto CreateDto(
        string name = "Wireless Mouse",
        string sku = "MOUSE-100",
        string description = "Wireless ergonomic mouse",
        decimal price = 29.99m,
        int stockQuantity = 25,
        int categoryId = 1)
    {
        return new CreateProductDto
        {
            Name = name,
            Sku = sku,
            Description = description,
            Price = price,
            StockQuantity = stockQuantity,
            CategoryId = categoryId
        };
    }
}
