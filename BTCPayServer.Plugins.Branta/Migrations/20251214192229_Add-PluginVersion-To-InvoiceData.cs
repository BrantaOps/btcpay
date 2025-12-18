using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BTCPayServer.Plugins.Branta.Migrations
{
    /// <inheritdoc />
    public partial class AddPluginVersionToInvoiceData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PluginVersion",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PluginVersion",
                schema: "BTCPayServer.Plugins.Branta",
                table: "Invoice");
        }
    }
}
