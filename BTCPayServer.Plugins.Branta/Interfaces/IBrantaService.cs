using BTCPayServer.Models.InvoicingModels;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Branta.Interfaces;

public interface IBrantaService
{
    Task<string> CreateInvoiceIfNotExistsAsync(CheckoutModel checkoutModel);
}
