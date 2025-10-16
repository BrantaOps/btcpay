using BTCPayServer.Models.InvoicingModels;
using BTCPayServer.Payments;
using BTCPayServer.Plugins.Branta.Classes;
using BTCPayServer.Plugins.Branta.Data.Domain;
using BTCPayServer.Plugins.Branta.Enums;
using BTCPayServer.Plugins.Branta.Interfaces;
using BTCPayServer.Plugins.Branta.Models;
using BTCPayServer.Plugins.Branta.Services;
using BTCPayServer.Plugins.Branta.Tests.Classes;
using BTCPayServer.Services.Invoices;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using System.Net;

namespace BTCPayServer.Plugins.Branta.Tests.Services;

public class BrantaServiceTests
{
    private readonly Mock<ILogger<BrantaService>> _loggerMock;
    private readonly Mock<IInvoiceService> _invoiceServiceMock;
    private readonly Mock<IInvoiceRepository> _invoiceRepositoryMock;
    private readonly Mock<IBrantaSettingsService> _brantaSettingsServiceMock;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<BrantaClient> _brantaClientMock;
    private readonly BrantaService _brantaService;

    private const string ValidApiKey = "valid-api-key-123";
    private const string OnChainAddress = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";
    private const string LightningAddress = "lnbc15u1p3xnhl2pp5jptserfk3zk4qy42tlucycrfwxhydvlemu9pqr93tuzlv9cc7g3sdqsvfhkcap3xyhx7un8cqzpgxqzjcsp5f8c52y2stc300gl6s4xswtjpc37hrnnr3c9wvtgjfuvqmpm35evq9qyyssqy4lgd8tj637qcjp05rdpxxykjenthxftej7a2zzmwrmrl70fyj9hvj0rewhzj7jfyuwkwcg9g2jpwtk3wkjtwnkdks84hsnu8xps5vsq4gj5hs";

    public BrantaServiceTests()
    {
        _loggerMock = new Mock<ILogger<BrantaService>>();
        _invoiceServiceMock = new Mock<IInvoiceService>();
        _invoiceRepositoryMock = new Mock<IInvoiceRepository>();
        _brantaSettingsServiceMock = new Mock<IBrantaSettingsService>();

        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage request, CancellationToken token) =>
            {
                var authHeader = request.Headers.Authorization;

                if (authHeader?.Parameter == ValidApiKey)
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.Created
                    };
                }
                else
                {
                    return new HttpResponseMessage
                    {
                        StatusCode = HttpStatusCode.Unauthorized
                    };
                }
            });
        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        httpClientFactoryMock
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(_httpMessageHandlerMock.Object));

        _brantaClientMock = new Mock<BrantaClient>(httpClientFactoryMock.Object);

        _brantaService = new BrantaService(
            _loggerMock.Object,
            _invoiceServiceMock.Object,
            _invoiceRepositoryMock.Object,
            _brantaSettingsServiceMock.Object,
            _brantaClientMock.Object
        );
    }

    [Fact]
    public async Task CreateInvoiceIfNotExists_CreatesInvoiceWhenBrantaDisabled()
    {
        var invoice = CreateInvoice();
        var checkoutModel = CreateCheckoutModel(invoice);

        SetSettings(invoice.StoreId, enabled: false);

        var result = await _brantaService.CreateInvoiceIfNotExistsAsync(checkoutModel);

        Assert.Null(result);
        _invoiceServiceMock.Verify(
            x => x.AddAsync(It.IsAny<InvoiceData>()),
            Times.Once
        );

        var resultInvoiceData = GetSavedInvoiceData();

        Assert.NotNull(resultInvoiceData);
        Assert.Equal("Branta is Disabled.", resultInvoiceData.FailureReason);
        Assert.Equal(checkoutModel.InvoiceId, resultInvoiceData.InvoiceId);
        Assert.Equal(checkoutModel.StoreId, resultInvoiceData.StoreId);
        Assert.Equal(InvoiceDataStatus.None, resultInvoiceData.Status);
    }

    [Fact]
    public async Task CreateInvoiceIfNotExists_CreatesInvoiceWhenBrantaAPIKeyInvalid()
    {
        var invoice = CreateInvoice();
        var checkoutModel = CreateCheckoutModel(invoice);

        SetSettings(invoice.StoreId, productionApiKey: null);

        var result = await _brantaService.CreateInvoiceIfNotExistsAsync(checkoutModel);

        Assert.Null(result);
        _invoiceServiceMock.Verify(
            x => x.AddAsync(It.IsAny<InvoiceData>()),
            Times.Once
        );

        var resultInvoiceData = GetSavedInvoiceData();

        Assert.NotNull(resultInvoiceData);
        Assert.Equal("Unauthorized", resultInvoiceData.FailureReason);
        Assert.Equal(InvoiceDataStatus.Failure, resultInvoiceData.Status);
    }

    [Fact]
    public async Task CreateInvoiceIfNotExists_CreatesInvoice()
    {
        var invoice = CreateInvoice();
        var checkoutModel = CreateCheckoutModel(invoice);

        SetSettings(invoice.StoreId);

        var result = await _brantaService.CreateInvoiceIfNotExistsAsync(checkoutModel);

        Assert.Contains(OnChainAddress, result);
        _invoiceServiceMock.Verify(
            x => x.AddAsync(It.IsAny<InvoiceData>()),
            Times.Once
        );

        var resultInvoiceData = GetSavedInvoiceData();

        Assert.NotNull(resultInvoiceData);
        Assert.Null(resultInvoiceData.FailureReason);
        Assert.Equal(InvoiceDataStatus.Success, resultInvoiceData.Status);
    }

    [Fact]
    public async Task CreateInvoiceIfNotExists_CreatesZeroKnowledgeInvoice()
    {
        var invoice = CreateInvoice();
        var checkoutModel = CreateCheckoutModel(invoice);

        SetSettings(invoice.StoreId, enableZeroKnowledge: true);

        var result = await _brantaService.CreateInvoiceIfNotExistsAsync(checkoutModel);

        var secret = TestHelper.GetSecret(result);
        var value = TestHelper.GetValueFromZeroKnowledgeUrl(result);
        Assert.NotNull(value);
        var decryptedValue = TestHelper.Decrypt(value, secret);
        Assert.Equal(OnChainAddress, decryptedValue);

        _invoiceServiceMock.Verify(
            x => x.AddAsync(It.IsAny<InvoiceData>()),
            Times.Once
        );

        var resultInvoiceData = GetSavedInvoiceData();

        Assert.NotNull(resultInvoiceData);
        Assert.Null(resultInvoiceData.FailureReason);
        Assert.Equal(InvoiceDataStatus.Success, resultInvoiceData.Status);
    }

    [Fact]
    public async Task CreateInvoiceIfNotExists_ProperlyAddsQueryParamsForLightningOnly()
    {
        var invoice = CreateInvoice("BTC-Lightning");
        var checkoutModel = CreateCheckoutModel(invoice);

        SetSettings(invoice.StoreId, enableZeroKnowledge: true);

        await _brantaService.CreateInvoiceIfNotExistsAsync(checkoutModel);

        Assert.Contains("?branta_payment_id", checkoutModel.InvoiceBitcoinUrlQR);
        Assert.Contains("&branta_zk_secret", checkoutModel.InvoiceBitcoinUrlQR);
    }

    [Fact]
    public async Task CreateInvoiceIfNotExists_ShouldNotSetZeroKnowledgeIfRequestUnsuccessful()
    {
        var invoice = CreateInvoice();
        var checkoutModel = CreateCheckoutModel(invoice);

        SetSettings(invoice.StoreId, enableZeroKnowledge: true, productionApiKey: "invalid-api-key");

        await _brantaService.CreateInvoiceIfNotExistsAsync(checkoutModel);

        Assert.DoesNotContain("branta_payment_id", checkoutModel.InvoiceBitcoinUrlQR);
        Assert.DoesNotContain("&branta_zk_secret", checkoutModel.InvoiceBitcoinUrlQR);
    }


    private InvoiceData GetSavedInvoiceData()
    {
        return _invoiceServiceMock.Invocations
            .First(i => i.Method.Name == nameof(IInvoiceService.AddAsync))
            .Arguments[0] as InvoiceData ?? throw new NullReferenceException();
    }

    private void SetSettings(
        string storeId,
        bool enabled = true,
        string? productionApiKey = ValidApiKey,
        bool enableZeroKnowledge = false)
    {
        var brantaSettings = new BrantaSettings()
        {
            BrantaEnabled = enabled,
            ProductionApiKey = productionApiKey,
            PostDescriptionEnabled = true,
            EnableZeroKnowledge = enableZeroKnowledge
        };

        _brantaSettingsServiceMock
            .Setup(x => x.GetAsync(storeId))
            .ReturnsAsync(brantaSettings);
    }

    private InvoiceEntity CreateInvoice(string paymentMethodId = "BTC")
    {
        var invoice = new InvoiceEntity()
        {
            Id = "123",
            StoreId = "XYZ",
            Metadata = new InvoiceMetadata
            {
                OrderId = "456",
                ItemDesc = "Description"
            }
        };

        var btcPaymentMethodId = PaymentMethodId.Parse(paymentMethodId);
        invoice.SetPaymentPrompt(btcPaymentMethodId, new PaymentPrompt()
        {
            Destination = paymentMethodId.Contains("Lightning") ? LightningAddress : OnChainAddress,
            PaymentMethodId = btcPaymentMethodId,
            Currency = "BTC"
        });

        _invoiceRepositoryMock
            .Setup(x => x.GetInvoice(invoice.Id))
            .ReturnsAsync(invoice);

        return invoice;
    }

    private static CheckoutModel CreateCheckoutModel(InvoiceEntity invoice)
    {
        return new CheckoutModel()
        {
            StoreId = invoice.StoreId,
            InvoiceId = invoice.Id,
            InvoiceBitcoinUrlQR = $"bitcoin:{OnChainAddress}"
        };
    }
}
