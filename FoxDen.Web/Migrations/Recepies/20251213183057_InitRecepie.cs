using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FoxDen.Web.Migrations.Recepies
{
    /// <inheritdoc />
    public partial class InitRecepie : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RecepieGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepieGroups", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecepieImages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Url = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepieImages", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecepieIngredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepieIngredients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecepieVersions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Servings = table.Column<int>(type: "INTEGER", nullable: false),
                    PhotoId = table.Column<int>(type: "INTEGER", nullable: true),
                    Rating = table.Column<byte>(type: "INTEGER", nullable: false),
                    CreatedUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    RecepieGroupId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepieVersions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecepieVersions_RecepieGroups_RecepieGroupId",
                        column: x => x.RecepieGroupId,
                        principalTable: "RecepieGroups",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecepieVersions_RecepieImages_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "RecepieImages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "RecepieIngredientSubstitutions",
                columns: table => new
                {
                    RecepieIngredientId = table.Column<int>(type: "INTEGER", nullable: false),
                    SubstitutionsId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepieIngredientSubstitutions", x => new { x.RecepieIngredientId, x.SubstitutionsId });
                    table.ForeignKey(
                        name: "FK_RecepieIngredientSubstitutions_RecepieIngredients_RecepieIngredientId",
                        column: x => x.RecepieIngredientId,
                        principalTable: "RecepieIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RecepieIngredientSubstitutions_RecepieIngredients_SubstitutionsId",
                        column: x => x.SubstitutionsId,
                        principalTable: "RecepieIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecepieProcesses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Description = table.Column<string>(type: "TEXT", maxLength: 2000, nullable: false),
                    Duration = table.Column<TimeSpan>(type: "TEXT", nullable: false),
                    PhotoId = table.Column<int>(type: "INTEGER", nullable: true),
                    RecepieVersionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepieProcesses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecepieProcesses_RecepieImages_PhotoId",
                        column: x => x.PhotoId,
                        principalTable: "RecepieImages",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecepieProcesses_RecepieVersions_RecepieVersionId",
                        column: x => x.RecepieVersionId,
                        principalTable: "RecepieVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RecepieItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IngredientId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantity = table.Column<float>(type: "REAL", nullable: false),
                    QuantityType = table.Column<int>(type: "INTEGER", nullable: false),
                    RecepieProcessId = table.Column<int>(type: "INTEGER", nullable: true),
                    RecepieVersionId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecepieItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecepieItems_RecepieIngredients_IngredientId",
                        column: x => x.IngredientId,
                        principalTable: "RecepieIngredients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecepieItems_RecepieProcesses_RecepieProcessId",
                        column: x => x.RecepieProcessId,
                        principalTable: "RecepieProcesses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RecepieItems_RecepieVersions_RecepieVersionId",
                        column: x => x.RecepieVersionId,
                        principalTable: "RecepieVersions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RecepieIngredientSubstitutions_SubstitutionsId",
                table: "RecepieIngredientSubstitutions",
                column: "SubstitutionsId");

            migrationBuilder.CreateIndex(
                name: "IX_RecepieItems_IngredientId",
                table: "RecepieItems",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_RecepieItems_RecepieProcessId",
                table: "RecepieItems",
                column: "RecepieProcessId");

            migrationBuilder.CreateIndex(
                name: "IX_RecepieItems_RecepieVersionId",
                table: "RecepieItems",
                column: "RecepieVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecepieProcesses_PhotoId",
                table: "RecepieProcesses",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecepieProcesses_RecepieVersionId",
                table: "RecepieProcesses",
                column: "RecepieVersionId");

            migrationBuilder.CreateIndex(
                name: "IX_RecepieVersions_PhotoId",
                table: "RecepieVersions",
                column: "PhotoId");

            migrationBuilder.CreateIndex(
                name: "IX_RecepieVersions_RecepieGroupId",
                table: "RecepieVersions",
                column: "RecepieGroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RecepieIngredientSubstitutions");

            migrationBuilder.DropTable(
                name: "RecepieItems");

            migrationBuilder.DropTable(
                name: "RecepieIngredients");

            migrationBuilder.DropTable(
                name: "RecepieProcesses");

            migrationBuilder.DropTable(
                name: "RecepieVersions");

            migrationBuilder.DropTable(
                name: "RecepieGroups");

            migrationBuilder.DropTable(
                name: "RecepieImages");
        }
    }
}
