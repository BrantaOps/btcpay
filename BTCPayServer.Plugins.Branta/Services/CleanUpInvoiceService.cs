using BTCPayServer.HostedServices;
using BTCPayServer.Plugins.Branta.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Branta.Services;

public class CleanUpInvoiceService(ILogger<CleanUpInvoiceService> logger, IInvoiceService invoiceService) : IPeriodicTask
{
    public const int DeleteAfterDays = 7;

    public async Task Do(CancellationToken cancellationToken)
    {
        var date = DateTime.UtcNow.AddDays(-DeleteAfterDays);

        var deleteCount = await invoiceService.DeleteInvoicesOlderThanAsync(date);

        logger.LogInformation($"BTCPayServer.Plugins.Branta: Purged {deleteCount} log records older than {DeleteAfterDays} days.");
    }
}
