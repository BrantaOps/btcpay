using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTCPayServer.Plugins.Branta.Migrations
{
    /// <inheritdoc />
    public partial class AddInvoiceIdToInvoice : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InvoiceId",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "InvoiceId",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice");
        }
    }
}
