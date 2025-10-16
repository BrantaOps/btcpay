using BTCPayServer.Payments;
using BTCPayServer.Plugins.Branta.Classes;

namespace BTCPayServer.Plugins.Branta.Services;

public class BrantaCheckoutModelExtension : IGlobalCheckoutModelExtension
{
    public void ModifyCheckoutModel(CheckoutModelContext context)
    {
        var paymentId = context.InvoiceEntity.Metadata.GetAdditionalData<string>(Constants.PaymentId);
        var secret = context.InvoiceEntity.Metadata.GetAdditionalData<string>(Constants.ZeroKnowledgeSecret);

        context.Model.SetZeroKnowledgeParams(paymentId, secret);
    }
}
