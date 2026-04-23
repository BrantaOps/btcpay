using BTCPayServer.HostedServices;
using BTCPayServer.Plugins.Branta.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Branta.Services;

public class CleanUpInvoiceService(
    ILogger<CleanUpInvoiceService> logger,
    IServiceScopeFactory serviceScopeFactory) : IPeriodicTask
{
    public const int DeleteAfterDays = 7;

    public async Task Do(CancellationToken cancellationToken)
    {
        using var scope = serviceScopeFactory.CreateScope();
        var invoiceService = scope.ServiceProvider.GetRequiredService<IInvoiceService>();

        var date = DateTime.UtcNow.AddDays(-DeleteAfterDays);
        var deleteCount = await invoiceService.DeleteInvoicesOlderThanAsync(date);

        logger.LogInformation(
            "BTCPayServer.Plugins.Branta: Purged {DeleteCount} log records older than {DeleteAfterDays} days.",
            deleteCount, DeleteAfterDays);
    }
}
