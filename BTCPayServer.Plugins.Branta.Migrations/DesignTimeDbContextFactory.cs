using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BTCPayServer.Plugins.Branta.Migrations;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<BrantaDbContext>
{
    public BrantaDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<BrantaDbContext>();
        builder.UseNpgsql("User ID=postgres;Host=127.0.0.1;Port=39372;Database=designtimebtcpay",
            b => b.MigrationsAssembly("BTCPayServer.Plugins.Branta"));
        return new BrantaDbContext(builder.Options, true);
    }
}
