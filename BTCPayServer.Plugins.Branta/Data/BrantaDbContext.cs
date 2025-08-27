using BTCPayServer.Plugins.Branta.Data.Domain;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.Branta;

public class BrantaDbContext : DbContext
{
    private readonly bool _designTime;

    public BrantaDbContext()
    {
    }

    public BrantaDbContext(DbContextOptions<BrantaDbContext> options, bool designTime = false)
        : base(options)
    {
        _designTime = designTime;
    }

    public DbSet<InvoiceData> Invoice { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("BTCPayServer.Plugins.Branta");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (!options.IsConfigured)
        {
            options.UseNpgsql("Server=localhost;Database=Branta;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");
        }
    }
}
