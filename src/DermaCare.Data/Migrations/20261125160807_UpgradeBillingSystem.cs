using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DermaCare.Data.Migrations
{
    /// <inheritdoc />
    public partial class UpgradeBillingSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BillNumber",
                table: "Invoices",
                type: "nvarchar(30)",
                maxLength: 30,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Invoices",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ItemType",
                table: "InvoiceItems",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "MedicineId",
                table: "InvoiceItems",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InvoiceItems_MedicineId",
                table: "InvoiceItems",
                column: "MedicineId");

            migrationBuilder.AddForeignKey(
                name: "FK_InvoiceItems_Medicines_MedicineId",
                table: "InvoiceItems",
                column: "MedicineId",
                principalTable: "Medicines",
                principalColumn: "MedicineId",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InvoiceItems_Medicines_MedicineId",
                table: "InvoiceItems");

            migrationBuilder.DropIndex(
                name: "IX_InvoiceItems_MedicineId",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "BillNumber",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Invoices");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "InvoiceItems");

            migrationBuilder.DropColumn(
                name: "MedicineId",
                table: "InvoiceItems");
        }
    }
}
