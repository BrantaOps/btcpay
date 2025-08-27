using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTCPayServer.Plugins.Branta.Migrations
{
    /// <inheritdoc />
    public partial class RenamePaymentUuidReplaceDateUpdatedWithProcessingTimeToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateUpdated",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice");

            migrationBuilder.RenameColumn(
                name: "PaymentUuid",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice",
                newName: "PaymentId");

            migrationBuilder.AddColumn<int>(
                name: "ProcessingTime",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessingTime",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice");

            migrationBuilder.RenameColumn(
                name: "PaymentId",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice",
                newName: "PaymentUuid");

            migrationBuilder.AddColumn<DateTime>(
                name: "DateUpdated",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
