using BTCPayServer.Plugins.Branta.Interfaces;
using BTCPayServer.Plugins.Branta.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;
using InvoiceData = BTCPayServer.Plugins.Branta.Data.Domain.InvoiceData;

namespace BTCPayServer.Plugins.Branta.Services;

public class InvoiceService(BrantaDbContext context, IBrantaSettingsService brantaSettingsService) : IInvoiceService
{
    public async Task<InvoiceDataViewModel> GetAsync(string storeId, int pageSize, int page)
    {
        var brantaSettings = await brantaSettingsService.GetAsync(storeId);

        if (page < 1) page = 1;

        var query = context.Invoice
            .Where(i => i.StoreId == storeId);

        var totalCount = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        var invoices = (await query
            .OrderByDescending(i => i.DateCreated)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync())
            .Select(i => new InvoiceDto(i, brantaSettings))
            .ToList();

        return new InvoiceDataViewModel
        {
            Invoices = invoices,
            CurrentPage = page,
            TotalPages = totalPages
        };
    }

    public async Task<InvoiceData> GetAsync(string invoiceId)
    {
        return await context.Invoice
            .FirstOrDefaultAsync(i => i.InvoiceId == invoiceId);
    }

    public async Task<InvoiceData> AddAsync(InvoiceData invoiceData)
    {
        await context.Invoice.AddAsync(invoiceData);
        await context.SaveChangesAsync();

        return invoiceData;
    }

    public async Task<int> DeleteInvoicesOlderThanAsync(DateTime date)
    {
        var invoices = await context.Invoice
            .Where(i => i.ExpirationDate < date)
            .ToListAsync();

        context.Invoice.RemoveRange(invoices);
        await context.SaveChangesAsync();

        return invoices.Count;
    }
}
