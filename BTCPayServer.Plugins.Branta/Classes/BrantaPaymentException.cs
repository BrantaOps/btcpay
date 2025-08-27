using System;

namespace BTCPayServer.Plugins.Branta.Classes;

public class BrantaPaymentException(string message) : Exception(message)
{
}
