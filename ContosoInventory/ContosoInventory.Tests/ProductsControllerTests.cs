using ContosoInventory.Server.Controllers;
using ContosoInventory.Server.Services;
using ContosoInventory.Shared.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ContosoInventory.Tests;

public sealed class ProductsControllerTests
{
    private readonly Mock<IProductService> _productServiceMock = new();
    private readonly ProductsController _controller;

    public ProductsControllerTests()
    {
        _controller = new ProductsController(
            _productServiceMock.Object,
            Mock.Of<ILogger<ProductsController>>());
    }

    [Fact]
    public async Task GetAllProducts_CategoryFilter_ReturnsOkAndForwardsCategory()
    {
        _productServiceMock
            .Setup(service => service.GetAllProductsAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ProductResponseDto>());

        var result = await _controller.GetAllProducts(2, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result);
        _productServiceMock.Verify(
            service => service.GetAllProductsAsync(2, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetProductById_MissingProduct_ReturnsNotFound()
    {
        _productServiceMock
            .Setup(service => service.GetProductByIdAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponseDto?)null);

        var result = await _controller.GetProductById(42, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetProductById_ExistingProduct_ReturnsOk()
    {
        var product = CreateProductResponse(12);
        _productServiceMock
            .Setup(service => service.GetProductByIdAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _controller.GetProductById(12, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(product, ok.Value);
    }

    [Fact]
    public async Task CreateProduct_ValidProduct_ReturnsCreatedAtActionWithLocation()
    {
        var product = CreateProductResponse(12);
        _productServiceMock
            .Setup(service => service.CreateProductAsync(It.IsAny<CreateProductDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _controller.CreateProduct(new CreateProductDto(), CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.Equal(nameof(ProductsController.GetProductById), created.ActionName);
        Assert.Equal(12, created.RouteValues?["id"]);
        Assert.Same(product, created.Value);
    }

    [Fact]
    public async Task CreateProduct_DuplicateSku_ReturnsConflict()
    {
        _productServiceMock
            .Setup(service => service.CreateProductAsync(It.IsAny<CreateProductDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProductSkuConflictException("ABC-123"));

        var result = await _controller.CreateProduct(new CreateProductDto(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task CreateProduct_InvalidCategory_ReturnsBadRequest()
    {
        _productServiceMock
            .Setup(service => service.CreateProductAsync(It.IsAny<CreateProductDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentException("Category does not exist."));

        var result = await _controller.CreateProduct(new CreateProductDto(), CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task UpdateProduct_MissingProduct_ReturnsNotFound()
    {
        _productServiceMock
            .Setup(service => service.UpdateProductAsync(
                42,
                It.IsAny<UpdateProductDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponseDto?)null);

        var result = await _controller.UpdateProduct(42, new UpdateProductDto(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdateProduct_ValidProduct_ReturnsOk()
    {
        var product = CreateProductResponse(12);
        _productServiceMock
            .Setup(service => service.UpdateProductAsync(
                12,
                It.IsAny<UpdateProductDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _controller.UpdateProduct(12, new UpdateProductDto(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(product, ok.Value);
    }

    [Fact]
    public async Task UpdateProduct_DuplicateSku_ReturnsConflict()
    {
        _productServiceMock
            .Setup(service => service.UpdateProductAsync(
                12,
                It.IsAny<UpdateProductDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ProductSkuConflictException("ABC-123"));

        var result = await _controller.UpdateProduct(12, new UpdateProductDto(), CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    [Fact]
    public async Task DeleteProduct_ExistingProduct_ReturnsNoContent()
    {
        _productServiceMock
            .Setup(service => service.DeleteProductAsync(12, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var result = await _controller.DeleteProduct(12, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task DeleteProduct_MissingProduct_ReturnsNotFound()
    {
        _productServiceMock
            .Setup(service => service.DeleteProductAsync(42, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _controller.DeleteProduct(42, CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task RestockProduct_InvalidQuantity_ReturnsBadRequest()
    {
        _productServiceMock
            .Setup(service => service.RestockProductAsync(
                12,
                It.IsAny<RestockProductDto>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ArgumentOutOfRangeException("quantity"));

        var result = await _controller.RestockProduct(
            12,
            new RestockProductDto { Quantity = 0 },
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task RestockProduct_ExistingProduct_ReturnsOk()
    {
        var product = CreateProductResponse(12);
        _productServiceMock
            .Setup(service => service.RestockProductAsync(
                12,
                It.IsAny<RestockProductDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        var result = await _controller.RestockProduct(
            12,
            new RestockProductDto { Quantity = 10 },
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.Same(product, ok.Value);
    }

    [Fact]
    public async Task RestockProduct_MissingProduct_ReturnsNotFound()
    {
        _productServiceMock
            .Setup(service => service.RestockProductAsync(
                42,
                It.IsAny<RestockProductDto>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProductResponseDto?)null);

        var result = await _controller.RestockProduct(
            42,
            new RestockProductDto { Quantity = 1 },
            CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task DeleteCategory_ProductsReferenceCategory_ReturnsConflict()
    {
        var categoryService = new Mock<ICategoryService>();
        categoryService
            .Setup(service => service.DeleteCategoryAsync(1, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new CategoryInUseException(1));
        var controller = new CategoriesController(
            categoryService.Object,
            Mock.Of<ILogger<CategoriesController>>());

        var result = await controller.DeleteCategory(1, CancellationToken.None);

        Assert.IsType<ConflictObjectResult>(result);
    }

    private static ProductResponseDto CreateProductResponse(int id)
    {
        return new ProductResponseDto
        {
            Id = id,
            Name = "Wireless Mouse",
            Sku = "MOUSE-100",
            Description = "Wireless ergonomic mouse",
            Price = 29.99m,
            StockQuantity = 25,
            CategoryId = 1,
            CreatedDate = DateTime.UtcNow,
            LastUpdatedDate = DateTime.UtcNow
        };
    }
}
