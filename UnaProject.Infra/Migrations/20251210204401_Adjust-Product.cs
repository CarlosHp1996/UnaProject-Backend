using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnaProject.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AdjustProduct : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Benefit",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "NutritionalInfo",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Benefit",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NutritionalInfo",
                table: "Products",
                type: "text",
                nullable: true);
        }
    }
}
