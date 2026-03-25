using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace UnaProject.Infra.Migrations
{
    /// <inheritdoc />
    public partial class AddMultipleProductImages : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrls",
                table: "Products",
                type: "text",
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrls\" = '[\"' || \"ImageUrl\" || '\"]' WHERE \"ImageUrl\" IS NOT NULL AND \"ImageUrl\" != '';");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrls\" = '[]' WHERE \"ImageUrl\" IS NULL OR \"ImageUrl\" = '';");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrls",
                table: "Products",
                type: "text",
                nullable: false,
                defaultValue: "[]",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Products");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = trim(both '\"[]' from \"ImageUrls\") WHERE \"ImageUrls\" IS NOT NULL AND \"ImageUrls\" != '[]';");
            migrationBuilder.Sql("UPDATE \"Products\" SET \"ImageUrl\" = '' WHERE \"ImageUrls\" IS NULL OR \"ImageUrls\" = '[]';");

            migrationBuilder.AlterColumn<string>(
                name: "ImageUrl",
                table: "Products",
                type: "character varying(500)",
                maxLength: 500,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(500)",
                oldMaxLength: 500,
                oldNullable: true);

            migrationBuilder.DropColumn(
                name: "ImageUrls",
                table: "Products");
        }
    }
}
