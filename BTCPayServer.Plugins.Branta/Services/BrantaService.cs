using BTCPayServer.Models.InvoicingModels;
using BTCPayServer.Plugins.Branta.Classes;
using BTCPayServer.Plugins.Branta.Interfaces;
using BTCPayServer.Services.Invoices;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using InvoiceData = BTCPayServer.Plugins.Branta.Data.Domain.InvoiceData;

namespace BTCPayServer.Plugins.Branta.Services;

public class BrantaService(
    ILogger<BrantaService> logger,
    IInvoiceService invoiceService,
    IInvoiceRepository invoiceRepository,
    IBrantaSettingsService brantaSettingsService,
    BrantaClient brantaClient) : IBrantaService
{
    public async Task<string> CreateInvoiceIfNotExistsAsync(CheckoutModel checkoutModel)
    {
        try
        {
            var btcPayInvoice = await invoiceRepository.GetInvoice(checkoutModel.InvoiceId);
            var brantaSettings = await brantaSettingsService.GetAsync(checkoutModel.StoreId);

            var brantaInvoice = await GetOrCreateBrantaInvoiceAsync(
                checkoutModel.InvoiceId,
                btcPayInvoice,
                brantaSettings
            );

            await AddZeroKnowledgeParametersIfNeededAsync(
                checkoutModel,
                brantaInvoice,
                brantaSettings,
                btcPayInvoice.Id
            );

            return GetVerifyLinkIfEnabled(brantaInvoice, brantaSettings);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error Code: B01");
            return null;
        }
    }
    private async Task<InvoiceData> GetOrCreateBrantaInvoiceAsync(
        string invoiceId,
        InvoiceEntity btcPayInvoice,
        Models.BrantaSettings settings)
    {
        return await invoiceService.GetAsync(invoiceId)
            ?? await CreateInvoiceAsync(btcPayInvoice, settings);
    }

    private async Task AddZeroKnowledgeParametersIfNeededAsync(
        CheckoutModel checkoutModel,
        InvoiceData brantaInvoice,
        Models.BrantaSettings settings,
        string btcPayInvoiceId)
    {
        if (!settings.EnableZeroKnowledge)
        {
            return;
        }

        var payload = new JObject
        {
            ["branta_payment_id"] = brantaInvoice.PaymentId,
            ["branta_zk_secret"] = brantaInvoice.ZeroKnowledgeSecret
        };

        var queryString = BuildQueryString(payload);
        checkoutModel.InvoiceBitcoinUrlQR += $"&{queryString}";

        await invoiceRepository.UpdateInvoiceMetadata(
            btcPayInvoiceId,
            checkoutModel.StoreId,
            payload
        );
    }

    private static string BuildQueryString(JObject payload)
    {
        return string.Join("&",
            payload.Properties().Select(p => $"{p.Name}={p.Value}")
        );
    }

    private static string GetVerifyLinkIfEnabled(
        InvoiceData invoice,
        Models.BrantaSettings settings)
    {
        return settings.ShowVerifyLink ? invoice?.GetVerifyLink() : null;
    }

    private async Task<InvoiceData> CreateInvoiceAsync(InvoiceEntity btcPayInvoice, Models.BrantaSettings brantaSettings)
    {
        var sw = Stopwatch.StartNew();

        var now = DateTime.UtcNow;

        var secret = brantaSettings.EnableZeroKnowledge ? Guid.NewGuid().ToString() : null;
        var payments = btcPayInvoice
            .GetPaymentPrompts()
            .Where(pp => pp.Destination != null)
            .Select(pp => pp.Destination)
            .Select(d => brantaSettings.EnableZeroKnowledge ? Helper.Encrypt(d, secret.ToString()) : d)
            .ToList();

        var invoiceData = new InvoiceData()
        {
            DateCreated = now,
            InvoiceId = btcPayInvoice.Id,
            PaymentId = payments
                .OrderBy(p => p.Length)
                .First(),
            Environment = brantaSettings.StagingEnabled ? Enums.ServerEnvironment.Staging : Enums.ServerEnvironment.Production,
            StoreId = btcPayInvoice.StoreId,
            ZeroKnowledgeSecret = secret
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
            var ttl = (btcPayInvoice.ExpirationTime.AddMinutes(brantaSettings.TTL) - btcPayInvoice.InvoiceTime).TotalSeconds;
            invoiceData.ExpirationDate = now.AddSeconds(ttl);

            var paymentRequest = new Classes.PaymentRequest()
            {
                payment = new Payment
                {
                    description = brantaSettings.PostDescriptionEnabled ? GetDescription(btcPayInvoice) : null,
                    payment = payments.First(),
                    alt_payments = [.. payments.Skip(1)],
                    ttl = ttl.ToString(),
                    btcPayServerPluginVersion = Helper.GetVersion(),
                    zk = brantaSettings.EnableZeroKnowledge
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

    private static string GetDescription(InvoiceEntity btcPayInvoice)
    {
        var orderId = btcPayInvoice.Metadata.OrderId;
        var description = btcPayInvoice.Metadata.ItemDesc;
        var descPart = string.IsNullOrWhiteSpace(description) ? "" : $" - {description}";

        return $"Order {orderId}{descPart}";
    }

}
