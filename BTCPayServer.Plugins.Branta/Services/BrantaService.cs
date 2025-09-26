using BTCPayServer.Models.InvoicingModels;
using BTCPayServer.Plugins.Branta.Classes;
using BTCPayServer.Plugins.Branta.Interfaces;
using BTCPayServer.Services.Invoices;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using InvoiceData = BTCPayServer.Plugins.Branta.Data.Domain.InvoiceData;

namespace BTCPayServer.Plugins.Branta.Services;

public class BrantaService(
    ILogger<BrantaService> logger,
    IInvoiceService invoiceService,
    InvoiceRepository invoiceRepository,
    IBrantaSettingsService brantaSettingsService,
    BrantaClient brantaClient) : IBrantaService
{
    public async Task<string> CreateInvoiceIfNotExistsAsync(CheckoutModel checkoutModel)
    {
        try
        {
            var brantaInvoice = await invoiceService.GetAsync(checkoutModel.InvoiceId) ??
                await CreateInvoiceAsync(checkoutModel);

            return brantaInvoice?.GetVerifyLink();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error Code: B01");
            return null;
        }
    }

    private async Task<InvoiceData> CreateInvoiceAsync(CheckoutModel checkoutModel)
    {
        var sw = Stopwatch.StartNew();

        var btcPayInvoice = await invoiceRepository.GetInvoice(checkoutModel.InvoiceId);

        var brantaSettings = await brantaSettingsService.GetAsync(btcPayInvoice.StoreId);

        var now = DateTime.UtcNow;

        var payments = btcPayInvoice
            .GetPaymentPrompts()
            .Where(pp => pp.Destination != null)
            .Select(pp => pp.Destination)
            .ToList();

        var invoiceData = new InvoiceData()
        {
            DateCreated = now,
            InvoiceId = btcPayInvoice.Id,
            PaymentId = payments.First(),
            Environment = Enums.ServerEnvironment.Production,
            StoreId = btcPayInvoice.StoreId,
        };

        if (!brantaSettings.BrantaEnabled)
        {
            invoiceData.FailureReason = "Branta is Disabled.";

            invoiceData.ProcessingTime = (int)sw.Elapsed.TotalMilliseconds;
            await invoiceService.AddAsync(invoiceData);

            return null;
        }

        try
        {
            var orderId = btcPayInvoice.Metadata.OrderId;
            var description = btcPayInvoice.Metadata.ItemDesc;
            var descPart = string.IsNullOrWhiteSpace(description) ? "" : $" - {description}";

            var ttl = (btcPayInvoice.ExpirationTime.AddMinutes(30) - btcPayInvoice.InvoiceTime).TotalSeconds;
            invoiceData.ExpirationDate = now.AddSeconds(ttl);

            var paymentRequest = new Classes.PaymentRequest()
            {
                payment = new Payment
                {
                    description = $"Order {orderId}{descPart}",
                    payment = payments.First(),
                    alt_payments = [.. payments.Skip(1)],
                    ttl = ttl.ToString(),
                    btcPayServerPluginVersion = Helper.GetVersion()
                }
            };

            await brantaClient.PostPaymentAsync(paymentRequest, brantaSettings);

            invoiceData.Status = Enums.InvoiceDataStatus.Success;
        }
        catch (BrantaPaymentException ex)
        {
            invoiceData.FailureReason = ex.Message;
            invoiceData.Status = Enums.InvoiceDataStatus.Failure;
        }
        catch (Exception ex)
        {
            invoiceData.FailureReason = "An unknown error occurred.";
            invoiceData.Status = Enums.InvoiceDataStatus.Failure;
            logger.LogError(ex, "Error Code: B02");
        }

        invoiceData.ProcessingTime = (int)sw.Elapsed.TotalMilliseconds;
        await invoiceService.AddAsync(invoiceData);

        return invoiceData;
    }
}
