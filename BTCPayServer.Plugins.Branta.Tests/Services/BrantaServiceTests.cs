using Branta.Classes;
using Branta.Exceptions;
using Branta.V2.Interfaces;
using Branta.V2.Models;
using BTCPayServer.Models.InvoicingModels;
using BTCPayServer.Payments;
using BTCPayServer.Plugins.Branta.Data.Domain;
using BTCPayServer.Plugins.Branta.Enums;
using BTCPayServer.Plugins.Branta.Interfaces;
using BTCPayServer.Plugins.Branta.Models;
using BTCPayServer.Plugins.Branta.Services;
using BTCPayServer.Plugins.Branta.Tests.Classes;
using BTCPayServer.Services.Invoices;
using Microsoft.Extensions.Logging;
using Moq;

namespace BTCPayServer.Plugins.Branta.Tests.Services;

public class BrantaServiceTests
{
    private readonly Mock<ILogger<BtcPayBrantaService>> _loggerMock;
    private readonly Mock<IInvoiceService> _invoiceServiceMock;
    private readonly Mock<IInvoiceRepository> _invoiceRepositoryMock;
    private readonly Mock<IBrantaSettingsService> _brantaSettingsServiceMock;
    private readonly Mock<IBrantaService> _brantaServiceMock;
    private readonly BtcPayBrantaService _btcPayBrantaService;

    private const string ValidApiKey = "valid-api-key-123";
    private const string OnChainAddress = "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa";
    private const string LightningAddress = "lnbc15u1p3xnhl2pp5jptserfk3zk4qy42tlucycrfwxhydvlemu9pqr93tuzlv9cc7g3sdqsvfhkcap3xyhx7un8cqzpgxqzjcsp5f8c52y2stc300gl6s4xswtjpc37hrnnr3c9wvtgjfuvqmpm35evq9qyyssqy4lgd8tj637qcjp05rdpxxykjenthxftej7a2zzmwrmrl70fyj9hvj0rewhzj7jfyuwkwcg9g2jpwtk3wkjtwnkdks84hsnu8xps5vsq4gj5hs";

    public BrantaServiceTests()
    {
        _loggerMock = new Mock<ILogger<BtcPayBrantaService>>();
        _invoiceServiceMock = new Mock<IInvoiceService>();
        _invoiceRepositoryMock = new Mock<IInvoiceRepository>();
        _brantaSettingsServiceMock = new Mock<IBrantaSettingsService>();
        _brantaServiceMock = new Mock<IBrantaService>();

        var zkEncryptedValue = AesEncryption.Encrypt(OnChainAddress, "1234");

        _brantaServiceMock
            .Setup(x => x.AddPaymentAsync(
                It.IsAny<Payment>(),
                It.Is<BrantaClientOptions>(o => o.DefaultApiKey != ValidApiKey),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new BrantaPaymentException("Unauthorized"));

        _brantaServiceMock
            .Setup(x => x.AddPaymentAsync(
                It.Is<Payment>(p => p.Destinations.All(d => !d.IsZk)),
                It.Is<BrantaClientOptions>(o => o.DefaultApiKey == ValidApiKey),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new Payment { Destinations = [], VerifyUrl = $"https://branta.pro/v2/verify/{OnChainAddress}" }, ""));

        _brantaServiceMock
            .Setup(x => x.AddPaymentAsync(
                It.Is<Payment>(p => p.Destinations.Any(d => d.IsZk)),
                It.Is<BrantaClientOptions>(o => o.DefaultApiKey == ValidApiKey),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((new Payment { Destinations = [], VerifyUrl = $"https://branta.pro/v2/verify/{Uri.EscapeDataString(zkEncryptedValue)}#k-xyz=1234" }, "1234"));

        _btcPayBrantaService = new BtcPayBrantaService(
            _loggerMock.Object,
            _invoiceServiceMock.Object,
            _invoiceRepositoryMock.Object,
            _brantaSettingsServiceMock.Object,
            _brantaServiceMock.Object
        );
    }

    [Fact]
    public async Task CreateInvoiceIfNotExists_CreatesInvoiceWhenBrantaDisabled()
    {
        var invoice = CreateInvoice();
        var checkoutModel = CreateCheckoutModel(invoice);

        SetSettings(invoice.StoreId, enabled: false);

        var result = await _btcPayBrantaService.CreateInvoiceIfNotExistsAsync(checkoutModel);

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

        var result = await _btcPayBrantaService.CreateInvoiceIfNotExistsAsync(checkoutModel);

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

        var result = await _btcPayBrantaService.CreateInvoiceIfNotExistsAsync(checkoutModel);

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

        SetSettings(invoice.StoreId);

        var result = await _btcPayBrantaService.CreateInvoiceIfNotExistsAsync(checkoutModel);

        var value = TestHelper.GetValueFromZeroKnowledgeUrl(result);
        Assert.NotNull(value);
        var decryptedValue = AesEncryption.Decrypt(value, "1234");
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
    public async Task CreateInvoiceIfNotExists_DoesNotAddZeroKnowledgeParamsBolt11()
    {
        var invoice = CreateInvoice("BTC-Lightning");
        var checkoutModel = CreateCheckoutModel(invoice);

        SetSettings(invoice.StoreId);

        await _btcPayBrantaService.CreateInvoiceIfNotExistsAsync(checkoutModel);

        Assert.DoesNotContain("?branta_id", checkoutModel.InvoiceBitcoinUrlQR);
        Assert.DoesNotContain("&branta_secret", checkoutModel.InvoiceBitcoinUrlQR);
    }

    [Fact]
    public async Task CreateInvoiceIfNotExists_ShouldNotSetZeroKnowledgeIfRequestUnsuccessful()
    {
        var invoice = CreateInvoice();
        var checkoutModel = CreateCheckoutModel(invoice);

        SetSettings(invoice.StoreId, productionApiKey: "invalid-api-key");

        await _btcPayBrantaService.CreateInvoiceIfNotExistsAsync(checkoutModel);

        Assert.DoesNotContain("branta_id", checkoutModel.InvoiceBitcoinUrlQR);
        Assert.DoesNotContain("&branta_secret", checkoutModel.InvoiceBitcoinUrlQR);
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
        string? productionApiKey = ValidApiKey)
    {
        var brantaSettings = new BrantaSettings()
        {
            BrantaEnabled = enabled,
            ProductionApiKey = productionApiKey,
            PostDescriptionEnabled = true
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
