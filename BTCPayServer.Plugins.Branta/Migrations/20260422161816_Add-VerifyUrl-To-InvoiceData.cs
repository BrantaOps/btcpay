using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTCPayServer.Plugins.Branta.Migrations
{
    /// <inheritdoc />
    public partial class AddVerifyUrlToInvoiceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "VerifyUrl",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VerifyUrl",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice");
        }
    }
}
