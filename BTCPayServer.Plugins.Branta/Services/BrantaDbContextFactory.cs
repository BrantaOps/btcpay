using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System;

namespace BTCPayServer.Plugins.Branta.Services;

public class BrantaDbContextFactory(IOptions<DatabaseOptions> options) : BaseDbContextFactory<BrantaDbContext>(options, "BTCPayServer.Plugins.Branta")
{
    public override BrantaDbContext CreateContext(Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
    {
        var builder = new DbContextOptionsBuilder<BrantaDbContext>();
        ConfigureBuilder(builder);
        return new BrantaDbContext(builder.Options);
    }
}
