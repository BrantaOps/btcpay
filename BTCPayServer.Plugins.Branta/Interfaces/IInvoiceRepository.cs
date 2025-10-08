using BTCPayServer.Services.Invoices;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Branta.Interfaces;
public interface IInvoiceRepository
{
    Task<InvoiceEntity> GetInvoice(string invoiceId);
}