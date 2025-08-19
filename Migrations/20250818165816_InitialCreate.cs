using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ProvaPub.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Customers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Numbers",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Number = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Numbers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Products",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Products", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Value = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CustomerId = table.Column<int>(type: "int", nullable: false),
                    OrderDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Customers_CustomerId",
                        column: x => x.CustomerId,
                        principalTable: "Customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "Customers",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Lorene Friesen" },
                    { 2, "Esther Cronin" },
                    { 3, "Angelica Tromp" },
                    { 4, "Brittany Wunsch" },
                    { 5, "Edgar Gislason" },
                    { 6, "Elvira Goyette" },
                    { 7, "Katrina Rodriguez" },
                    { 8, "Mable Kuvalis" },
                    { 9, "Beverly Cassin" },
                    { 10, "Clinton Jacobson" },
                    { 11, "Heather King" },
                    { 12, "Ethel Huel" },
                    { 13, "Veronica Hodkiewicz" },
                    { 14, "Christopher Ernser" },
                    { 15, "Nicole Ebert" },
                    { 16, "Ryan Blanda" },
                    { 17, "Sandra Huel" },
                    { 18, "Carlos Kuvalis" },
                    { 19, "Aubrey Ernser" },
                    { 20, "Lauren Hills" }
                });

            migrationBuilder.InsertData(
                table: "Products",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Refined Steel Table" },
                    { 2, "Handmade Soft Tuna" },
                    { 3, "Ergonomic Rubber Car" },
                    { 4, "Incredible Fresh Mouse" },
                    { 5, "Gorgeous Frozen Fish" },
                    { 6, "Small Concrete Fish" },
                    { 7, "Tasty Cotton Cheese" },
                    { 8, "Rustic Rubber Pants" },
                    { 9, "Handcrafted Plastic Chips" },
                    { 10, "Gorgeous Concrete Tuna" },
                    { 11, "Small Cotton Bike" },
                    { 12, "Practical Cotton Hat" },
                    { 13, "Unbranded Metal Cheese" },
                    { 14, "Rustic Cotton Pizza" },
                    { 15, "Tasty Frozen Car" },
                    { 16, "Intelligent Plastic Hat" },
                    { 17, "Licensed Metal Chips" },
                    { 18, "Unbranded Frozen Mouse" },
                    { 19, "Licensed Cotton Table" },
                    { 20, "Sleek Fresh Sausages" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Numbers_Number",
                table: "Numbers",
                column: "Number",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CustomerId",
                table: "Orders",
                column: "CustomerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Numbers");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Products");

            migrationBuilder.DropTable(
                name: "Customers");
        }
    }
}
