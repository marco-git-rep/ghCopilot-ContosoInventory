using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContosoInventory.Server.Migrations;

public partial class AddProducts : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Products",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Sku = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false, collation: "NOCASE"),
                Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                Price = table.Column<decimal>(type: "TEXT", precision: 8, scale: 2, nullable: false),
                StockQuantity = table.Column<int>(type: "INTEGER", nullable: false),
                CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastUpdatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", product => product.Id);
                table.ForeignKey(
                    name: "FK_Products_Categories_CategoryId",
                    column: product => product.CategoryId,
                    principalTable: "Categories",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Products_CategoryId",
            table: "Products",
            column: "CategoryId");

        migrationBuilder.CreateIndex(
            name: "IX_Products_Sku",
            table: "Products",
            column: "Sku",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "Products");
    }
}
