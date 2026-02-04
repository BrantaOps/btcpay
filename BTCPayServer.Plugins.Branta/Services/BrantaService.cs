using Branta.Classes;
using Branta.Enums;
using Branta.Exceptions;
using Branta.V2.Classes;
using Branta.V2.Models;
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
        if (!settings.EnableZeroKnowledge ||
            brantaInvoice.Status != Enums.InvoiceDataStatus.Success ||
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

    private static string GetVerifyLinkIfEnabled(
        InvoiceData invoice,
        Models.BrantaSettings settings)
    {
        return settings.ShowVerifyLink ? invoice?.GetVerifyLink(settings) : null;
    }

    private async Task<InvoiceData> CreateInvoiceAsync(InvoiceEntity btcPayInvoice, Models.BrantaSettings brantaSettings)
    {
        var sw = Stopwatch.StartNew();

        var now = DateTime.UtcNow;

        var chainBtcId = PaymentTypes.CHAIN.GetPaymentMethodId("BTC");
        var lnBtcId = PaymentTypes.LN.GetPaymentMethodId("BTC");

        var payments = btcPayInvoice
            .GetPaymentPrompts()
            .Where(pp => pp.Destination != null)
            .OrderBy(pp =>
            {
                if (pp.PaymentMethodId == chainBtcId) return 0;
                if (pp.PaymentMethodId == lnBtcId) return 1;

                if (pp.PaymentMethodId?.ToString().Contains("Lightning") == true ||
                    pp.PaymentMethodId?.ToString().Contains("LNURL") == true) return 2;

                return 3;
            })
            .Select(pp => new Destination
            {
                Value = pp.Destination,
                IsZk = brantaSettings.EnableZeroKnowledge && pp.PaymentMethodId == PaymentMethodId.TryParse("BTC")
            })
            .ToList();

        var invoiceData = new InvoiceData()
        {
            DateCreated = now,
            InvoiceId = btcPayInvoice.Id,
            PaymentId = payments
                .OrderBy(p => p.Value.Length)
                .First()
                .Value,
            Environment = brantaSettings.StagingEnabled ? BrantaServerBaseUrl.Staging : BrantaServerBaseUrl.Production,
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

        try
        {
            var ttl = (int)(btcPayInvoice.ExpirationTime.AddMinutes(brantaSettings.TTL) - btcPayInvoice.InvoiceTime).TotalSeconds;
            invoiceData.ExpirationDate = now.AddSeconds(ttl);

            var paymentRequest = new Payment()
            {
                Destinations = payments,
                Description = brantaSettings.PostDescriptionEnabled ? GetDescription(btcPayInvoice) : null,
                TTL = ttl,
                BtcPayServerPluginVersion = Helper.GetVersion()
            };

            var options = new BrantaClientOptions()
            {
                BaseUrl = brantaSettings.GetBrantaServerUrl(),
                DefaultApiKey = brantaSettings.GetAPIKey()
            };

            if (brantaSettings.EnableZeroKnowledge == true)
            {
                var (result, secret) = await brantaClient.AddZKPaymentAsync(paymentRequest, options);
                invoiceData.ZeroKnowledgeSecret = secret;
                invoiceData.PaymentId = result.Destinations.FirstOrDefault()?.Value;
            }
            else
            {
                await brantaClient.AddPaymentAsync(paymentRequest, options);
            }

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
