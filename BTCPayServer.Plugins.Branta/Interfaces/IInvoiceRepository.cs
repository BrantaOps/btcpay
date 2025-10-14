using BTCPayServer.Services.Invoices;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Branta.Interfaces;

public interface IInvoiceRepository
{
    Task<InvoiceEntity> GetInvoice(string invoiceId);

    Task<InvoiceEntity> UpdateInvoiceMetadata(string invoiceId, string storeId, JObject metadata);
}