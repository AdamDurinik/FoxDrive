using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoxDen.Web.Migrations
{
    /// <inheritdoc />
    public partial class InitShopping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShoppingItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Amount = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Date = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Shop = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Bought = table.Column<bool>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShoppingItems", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShoppingItems_Bought_Date",
                table: "ShoppingItems",
                columns: new[] { "Bought", "Date" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShoppingItems");
        }
    }
}
