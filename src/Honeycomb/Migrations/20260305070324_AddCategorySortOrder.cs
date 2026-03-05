using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Honeycomb.Migrations
{
    /// <inheritdoc />
    public partial class AddCategorySortOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SortOrder",
                table: "Categories",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            // Assign initial SortOrder based on Id order
            migrationBuilder.Sql(
                """
                UPDATE Categories
                SET SortOrder = (
                    SELECT COUNT(*) FROM Categories AS c2 WHERE c2.Id < Categories.Id
                )
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SortOrder",
                table: "Categories");
        }
    }
}
