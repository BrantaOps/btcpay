using Branta;
using BTCPayServer.Models.InvoicingModels;
using BTCPayServer.Payments;
using BTCPayServer.Plugins.Branta.Classes;
using BTCPayServer.Plugins.Branta.Interfaces;
using BTCPayServer.Services.Invoices;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using InvoiceData = BTCPayServer.Plugins.Branta.Data.Domain.InvoiceData;

namespace BTCPayServer.Plugins.Branta.Services;

public class BtcPayBrantaService(
    ILogger<BtcPayBrantaService> logger,
    IInvoiceService invoiceService,
    IInvoiceRepository invoiceRepository,
    IBrantaSettingsService brantaSettingsService,
    IBrantaService brantaService) : IBtcPayBrantaService
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

            return brantaSettings.ShowVerifyLink ? brantaInvoice?.VerifyUrl : null;
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
        if (brantaInvoice.Status != Enums.InvoiceDataStatus.Success ||
            checkoutModel.InvoiceBitcoinUrlQR.Contains(Constants.PaymentId))
        {
            return;
        }

        var existingInvoice = await invoiceRepository.GetInvoice(btcPayInvoiceId);
        var additionalData = existingInvoice?.Metadata?.AdditionalData
            ?? new Dictionary<string, JToken>();

        var payload = JObject.FromObject(additionalData);
        payload[Constants.PaymentId] = brantaInvoice.PaymentId;
        payload[Constants.ZeroKnowledgeSecret] = brantaInvoice.ZeroKnowledgeSecret;

        checkoutModel.SetZeroKnowledgeParams(brantaInvoice.PaymentId, brantaInvoice.ZeroKnowledgeSecret);

        await invoiceRepository.UpdateInvoiceMetadata(
            btcPayInvoiceId,
            checkoutModel.StoreId,
            payload
        );
    }

    private async Task<InvoiceData> CreateInvoiceAsync(InvoiceEntity btcPayInvoice, Models.BrantaSettings brantaSettings)
    {
        var sw = Stopwatch.StartNew();

        var now = DateTime.UtcNow;

        var chainBtcId = PaymentTypes.CHAIN.GetPaymentMethodId("BTC");
        var lnBtcId = PaymentTypes.LN.GetPaymentMethodId("BTC");
        var lnurlBtcId = PaymentTypes.LNURL.GetPaymentMethodId("BTC");

        var prompts = btcPayInvoice.GetPaymentPrompts().ToList();
        var skippedLnurl = prompts.Count(pp => pp.PaymentMethodId == lnurlBtcId);
        if (skippedLnurl > 0)
        {
            logger.LogInformation("Skipped {Count} LNURL-Pay payment prompt(s)", skippedLnurl);
        }

        var destinationPrompts = prompts
            .Where(pp => pp.Destination != null && pp.PaymentMethodId != lnurlBtcId)
            .OrderBy(pp =>
            {
                if (pp.PaymentMethodId == chainBtcId) return 0;
                if (pp.PaymentMethodId == lnBtcId) return 1;
                if (pp.PaymentMethodId?.ToString().Contains("Lightning") == true) return 2;
                return 3;
            })
            .ToList();

        var invoiceData = new InvoiceData()
        {
            DateCreated = now,
            InvoiceId = btcPayInvoice.Id,
            Environment = brantaSettings.StagingEnabled ? BrantaEnvironment.Staging : BrantaEnvironment.Production,
            StoreId = btcPayInvoice.StoreId,
            PluginVersion = Helper.GetVersion()
        };

        if (!brantaSettings.BrantaEnabled)
        {
            invoiceData.FailureReason = "Branta is Disabled.";

            invoiceData.ProcessingTime = (int)sw.Elapsed.TotalMilliseconds;
            await invoiceService.AddAsync(invoiceData);

            return null;
        }

        var ttl = (int)(btcPayInvoice.ExpirationTime.AddMinutes(brantaSettings.TTL) - btcPayInvoice.InvoiceTime).TotalSeconds;
        invoiceData.ExpirationDate = now.AddSeconds(ttl);

        if (destinationPrompts.Count == 0)
        {
            invoiceData.FailureReason = skippedLnurl > 0
                ? "LNURL and Lightning Address are not verified by Branta."
                : "No supported payment destinations to verify.";

            invoiceData.ProcessingTime = (int)sw.Elapsed.TotalMilliseconds;
            await invoiceService.AddAsync(invoiceData);

            return invoiceData;
        }

        try
        {
            var builder = new PaymentBuilder()
                .SetTtl(ttl)
                .SetBtcPayServerPluginVersion(Helper.GetVersion());

            if (brantaSettings.PostDescriptionEnabled)
            {
                builder.SetDescription(GetDescription(btcPayInvoice));
            }

            foreach (var pp in destinationPrompts)
            {
                builder.AddDestination(pp.Destination, GetDestinationType(pp.PaymentMethodId, chainBtcId, lnBtcId));
            }

            var options = new BrantaApiOptions()
            {
                Environment = brantaSettings.GetBrantaServerUrl(),
                ApiKey = brantaSettings.GetAPIKey()
            };

            var posted = await brantaService.AddPaymentAsync(builder.Build(), options);
            invoiceData.VerifyUrl = posted.VerifyUrl;
            var path = new Uri(posted.VerifyUrl).AbsolutePath;
            invoiceData.PaymentId = Uri.UnescapeDataString(path[(path.LastIndexOf('/') + 1)..]);
            invoiceData.ZeroKnowledgeSecret = posted.Secret;

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

    private static DestinationType? GetDestinationType(PaymentMethodId id, PaymentMethodId chainBtcId, PaymentMethodId lnBtcId)
    {
        if (id == chainBtcId)
            return DestinationType.BitcoinAddress;

        if (id == lnBtcId || id?.ToString().Contains("Lightning") == true)
            return DestinationType.Bolt11;

        if (id == new PaymentMethodId("ARKADE"))
            return DestinationType.ArkAddress;

        return null;
    }

    private static string GetDescription(InvoiceEntity btcPayInvoice)
    {
        var orderId = btcPayInvoice.Metadata.OrderId;
        var description = btcPayInvoice.Metadata.ItemDesc;
        var descPart = string.IsNullOrWhiteSpace(description) ? "" : $" - {description}";

        return $"Order {orderId}{descPart}";
    }
}
