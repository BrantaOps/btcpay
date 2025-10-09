using BTCPayServer.Plugins.Branta.Data.Domain;
using BTCPayServer.Plugins.Branta.Services;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.Branta.Tests.Services;

public class InvoiceServiceTests
{
    private readonly BrantaDbContext _context;
    private readonly InvoiceService _invoiceService;

    public InvoiceServiceTests()
    {
        var options = new DbContextOptionsBuilder<BrantaDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new BrantaDbContext(options);
        _invoiceService = new InvoiceService(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task GetAsync_WithValidStoreId_ReturnsCorrectPage()
    {
        var storeId = "store1";
        await SeedInvoices(storeId, 10);

        var result = await _invoiceService.GetAsync(storeId, pageSize: 5, page: 1);

        Assert.NotNull(result);
        Assert.Equal(5, result.Invoices.Count);
        Assert.Equal(1, result.CurrentPage);
        Assert.Equal(2, result.TotalPages);
    }

    [Fact]
    public async Task GetAsync_WithPageLessThanOne_DefaultsToPageOne()
    {
        var storeId = "store1";
        await SeedInvoices(storeId, 5);

        var result = await _invoiceService.GetAsync(storeId, pageSize: 5, page: 0);

        Assert.Equal(1, result.CurrentPage);
    }

    [Fact]
    public async Task GetAsync_OrdersByDateCreatedDescending()
    {
        var storeId = "store1";
        var oldInvoice = new InvoiceData 
        { 
            InvoiceId = "old", 
            StoreId = storeId, 
            DateCreated = DateTime.UtcNow.AddDays(-2) 
        };
        var newInvoice = new InvoiceData 
        { 
            InvoiceId = "new", 
            StoreId = storeId, 
            DateCreated = DateTime.UtcNow 
        };
        
        await _context.Invoice.AddRangeAsync(oldInvoice, newInvoice);
        await _context.SaveChangesAsync();

        var result = await _invoiceService.GetAsync(storeId, pageSize: 10, page: 1);

        Assert.Equal("new", result.Invoices.First().InvoiceId);
        Assert.Equal("old", result.Invoices.Last().InvoiceId);
    }

    [Fact]
    public async Task GetAsync_WithDifferentStoreId_ReturnsOnlyMatchingInvoices()
    {
        await SeedInvoices("store1", 5);
        await SeedInvoices("store2", 3);

        var result = await _invoiceService.GetAsync("store1", pageSize: 10, page: 1);

        Assert.Equal(5, result.Invoices.Count);
        Assert.All(result.Invoices, i => Assert.Equal("store1", i.StoreId));
    }

    [Fact]
    public async Task GetAsync_WithNoInvoices_ReturnsEmptyList()
    {
        var result = await _invoiceService.GetAsync("nonexistent", pageSize: 10, page: 1);

        Assert.Empty(result.Invoices);
        Assert.Equal(0, result.TotalPages);
    }

    [Fact]
    public async Task GetAsync_CalculatesTotalPagesCorrectly()
    {
        var storeId = "store1";
        await SeedInvoices(storeId, 23);

        var result = await _invoiceService.GetAsync(storeId, pageSize: 10, page: 1);

        Assert.Equal(3, result.TotalPages);
    }

    [Fact]
    public async Task GetAsync_WithValidInvoiceId_ReturnsInvoice()
    {
        var invoice = new InvoiceData 
        { 
            InvoiceId = "inv123", 
            StoreId = "store1" 
        };
        await _context.Invoice.AddAsync(invoice);
        await _context.SaveChangesAsync();

        var result = await _invoiceService.GetAsync("inv123");

        Assert.NotNull(result);
        Assert.Equal("inv123", result.InvoiceId);
    }

    [Fact]
    public async Task GetAsync_WithInvalidInvoiceId_ReturnsNull()
    {
        var result = await _invoiceService.GetAsync("nonexistent");

        Assert.Null(result);
    }

    [Fact]
    public async Task DeleteInvoicesOlderThanAsync_DeletesExpiredInvoices()
    {
        var expiredInvoice1 = new InvoiceData 
        { 
            InvoiceId = "exp1", 
            StoreId = "store1", 
            ExpirationDate = DateTime.UtcNow.AddDays(-2) 
        };
        var expiredInvoice2 = new InvoiceData 
        { 
            InvoiceId = "exp2", 
            StoreId = "store1", 
            ExpirationDate = DateTime.UtcNow.AddDays(-1) 
        };
        var validInvoice = new InvoiceData 
        { 
            InvoiceId = "valid1", 
            StoreId = "store1", 
            ExpirationDate = DateTime.UtcNow.AddDays(1) 
        };

        await _context.Invoice.AddRangeAsync(expiredInvoice1, expiredInvoice2, validInvoice);
        await _context.SaveChangesAsync();

        var deletedCount = await _invoiceService.DeleteInvoicesOlderThanAsync(DateTime.UtcNow);

        Assert.Equal(2, deletedCount);
        Assert.Equal(1, await _context.Invoice.CountAsync());
        Assert.NotNull(await _context.Invoice.FirstOrDefaultAsync(i => i.InvoiceId == "valid1"));
    }

    private async Task SeedInvoices(string storeId, int count)
    {
        var invoices = Enumerable.Range(1, count)
            .Select(i => new InvoiceData
            {
                InvoiceId = $"{storeId}_inv{i}",
                StoreId = storeId,
                DateCreated = DateTime.UtcNow.AddMinutes(-i)
            })
            .ToList();

        await _context.Invoice.AddRangeAsync(invoices);
        await _context.SaveChangesAsync();
    }

}
