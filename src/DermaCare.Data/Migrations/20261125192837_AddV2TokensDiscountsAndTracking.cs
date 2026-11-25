using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DermaCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddV2TokensDiscountsAndTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CollectedByUserId",
                table: "Invoices",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DiscountReason",
                table: "Invoices",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TokenNumber",
                table: "Appointments",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TokenSequence",
                table: "Appointments",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CollectedByUserId",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "DiscountReason",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "TokenNumber",
                table: "Appointments");

            migrationBuilder.DropColumn(
                name: "TokenSequence",
                table: "Appointments");
        }
    }
}
