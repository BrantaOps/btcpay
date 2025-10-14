using BTCPayServer.Payments;

namespace BTCPayServer.Plugins.Branta.Services;

public class BrantaCheckoutModelExtension : IGlobalCheckoutModelExtension
{
    public void ModifyCheckoutModel(CheckoutModelContext context)
    {
        var paymentId = context.InvoiceEntity.Metadata.GetAdditionalData<string>("branta_payment_id");
        var secret = context.InvoiceEntity.Metadata.GetAdditionalData<string>("branta_zk_secret");

        if (paymentId != null && secret != null)
        {
            context.Model.InvoiceBitcoinUrlQR +=
                (context.Model.InvoiceBitcoinUrlQR.Contains('?') ? "&" : "?") +
                $"branta_payment_id={paymentId}&branta_zk_secret={secret}";
        }
    }
}
