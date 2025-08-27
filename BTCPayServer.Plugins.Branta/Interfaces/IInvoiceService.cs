using BTCPayServer.Plugins.Branta.Enums;
using BTCPayServer.Plugins.Branta.Models;
using System;
using System.Threading.Tasks;
using InvoiceData = BTCPayServer.Plugins.Branta.Data.Domain.InvoiceData;

namespace BTCPayServer.Plugins.Branta.Interfaces;

public interface IInvoiceService
{
    Task<InvoiceDataViewModel> GetAsync(string storeId, int pageSize, int page);

    Task<InvoiceData> GetAsync(string invoiceId);

    Task<InvoiceData> AddAsync(InvoiceData invoiceData);

    Task<int> DeleteInvoicesOlderThanAsync(DateTime date);
}
