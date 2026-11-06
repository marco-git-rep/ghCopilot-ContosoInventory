using ContosoInventory.Server.Data;
using ContosoInventory.Server.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ContosoInventory.Tests;

public sealed class ProductMigrationTests
{
    [Fact]
    public async Task AddProductsMigration_CreatesProductTableAndCaseInsensitiveSkuIndex()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<InventoryContext>()
            .UseSqlite(connection)
            .Options;
        await using var context = new InventoryContext(options);

        await context.Database.MigrateAsync();

        var appliedMigrations = await context.Database.GetAppliedMigrationsAsync();
        Assert.Contains("20261106220000_AddProducts", appliedMigrations);

        var category = new Category
        {
            Name = "Peripherals",
            Description = "Computer accessories",
            DisplayOrder = 1,
            IsActive = true,
            CreatedDate = DateTime.UtcNow,
            LastModifiedDate = DateTime.UtcNow
        };
        context.Categories.Add(category);
        await context.SaveChangesAsync();

        context.Products.Add(CreateProduct(category.Id, "ABC-123"));
        await context.SaveChangesAsync();

        context.Products.Add(CreateProduct(category.Id, "abc-123"));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static Product CreateProduct(int categoryId, string sku)
    {
        var now = DateTime.UtcNow;
        return new Product
        {
            Name = "Wireless Mouse",
            Sku = sku,
            Description = "Wireless ergonomic mouse",
            Price = 29.99m,
            StockQuantity = 10,
            CategoryId = categoryId,
            CreatedDate = now,
            LastUpdatedDate = now
        };
    }
}
