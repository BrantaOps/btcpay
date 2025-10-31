using BTCPayServer.Plugins.Branta.Interfaces;
using BTCPayServer.Services.Invoices;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Branta.Services;

public class InvoiceRepositoryAdapter(InvoiceRepository repository) : IInvoiceRepository
{
    private readonly InvoiceRepository _repository = repository;

    public Task<InvoiceEntity> GetInvoice(string invoiceId)
    {
        return _repository.GetInvoice(invoiceId);
    }

    public Task<InvoiceEntity> UpdateInvoiceMetadata(string invoiceId, string storeId, JObject metadata)
    {
        return _repository.UpdateInvoiceMetadata(invoiceId, storeId, metadata);
    }
}