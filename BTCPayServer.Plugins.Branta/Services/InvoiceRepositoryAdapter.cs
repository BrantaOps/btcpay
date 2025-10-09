using BTCPayServer.Plugins.Branta.Interfaces;
using BTCPayServer.Services.Invoices;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Branta.Services;

public class InvoiceRepositoryAdapter(InvoiceRepository repository) : IInvoiceRepository
{
    private readonly InvoiceRepository _repository = repository;

    public Task<InvoiceEntity> GetInvoice(string invoiceId)
    {
        return _repository.GetInvoice(invoiceId);
    }
}