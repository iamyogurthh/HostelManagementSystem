using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HostelManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class ManualBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "FeeCharges",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Source",
                table: "FeeCharges",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Payments_ReceiptNumber",
                table: "Payments",
                column: "ReceiptNumber",
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Payments_TotalAmount_Positive",
                table: "Payments",
                sql: "TotalAmount > 0");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentAllocations_PaymentId_FeeChargeId",
                table: "PaymentAllocations",
                columns: new[] { "PaymentId", "FeeChargeId" },
                unique: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_PaymentAllocations_Amount_Positive",
                table: "PaymentAllocations",
                sql: "Amount > 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_FeeCharges_Amount_Positive",
                table: "FeeCharges",
                sql: "Amount > 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Payments_ReceiptNumber",
                table: "Payments");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Payments_TotalAmount_Positive",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_PaymentAllocations_PaymentId_FeeChargeId",
                table: "PaymentAllocations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_PaymentAllocations_Amount_Positive",
                table: "PaymentAllocations");

            migrationBuilder.DropCheckConstraint(
                name: "CK_FeeCharges_Amount_Positive",
                table: "FeeCharges");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "FeeCharges");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "FeeCharges");
        }
    }
}
