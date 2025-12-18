using BTCPayServer.Models.InvoicingModels;

namespace BTCPayServer.Plugins.Branta.Classes;

public static class Helper
{
    public static string GetVersion()
    {
        return typeof(BrantaPlugin).Assembly.GetName().Version?.ToString();
    }

    public static void SetZeroKnowledgeParams(this CheckoutModel model, string payment, string secret)
    {
        if (payment == null || secret == null || model.PaymentMethodId != "BTC-CHAIN")
        {
            return;
        }

        model.InvoiceBitcoinUrlQR +=
            (model.InvoiceBitcoinUrlQR.Contains('?') ? "&" : "?") +
            $"{Constants.PaymentId}={payment}&{Constants.ZeroKnowledgeSecret}={secret}";
    }
}
