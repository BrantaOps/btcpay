using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTCPayServer.Plugins.Branta.Migrations
{
    /// <inheritdoc />
    public partial class AddDetailsToInvoiceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "DateUpdated",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "Environment",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "FailureReason",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
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
                name: "DateCreated",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "DateUpdated",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "Environment",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "FailureReason",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice");
        }
    }
}
