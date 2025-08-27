using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Plugins.Branta.Classes;
using BTCPayServer.Plugins.Branta.Interfaces;
using BTCPayServer.Plugins.Branta.Services;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace BTCPayServer.Plugins.Branta;

public class BrantaPlugin : BaseBTCPayServerPlugin
{
    public override IBTCPayServerPlugin.PluginDependency[] Dependencies { get; } =
    {
        new IBTCPayServerPlugin.PluginDependency { Identifier = nameof(BTCPayServer), Condition = ">=2.0.0" }
    };

    public override void Execute(IServiceCollection services)
    {
        services.AddUIExtension("header-nav", "BrantaPluginHeaderNav");
        services.AddHostedService<BrantaMigrationRunner>();
        services.AddSingleton<BrantaDbContextFactory>();
        services.AddDbContext<BrantaDbContext>((provider, o) =>
        {
            var factory = provider.GetRequiredService<BrantaDbContextFactory>();
            factory.ConfigureBuilder(o);
        });

        services.AddScoped<IBrantaService, BrantaService>();
        services.AddScoped<IBrantaSettingsService, BrantaSettingsService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddHttpClient();
        services.AddScoped<BrantaClient>();
        services.AddScheduledTask<CleanUpInvoiceService>(TimeSpan.FromHours(24));

        services.AddUIExtension("checkout-payment-method", "Branta/VerifyWithBranta");
    }
}
