using BTCPayServer.Models.InvoicingModels;
using System;
using System.Security.Cryptography;
using System.Text;

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

    public static string Encrypt(string value, string secret)
    {
        byte[] keyData;
        using (var sha256 = SHA256.Create())
        {
            keyData = sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
        }

        byte[] iv = new byte[12];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(iv);
        }

        byte[] plaintext = Encoding.UTF8.GetBytes(value);
        byte[] ciphertext = new byte[plaintext.Length];
        byte[] tag = new byte[16];

        using (AesGcm aesGcm = new(keyData, 16))
        {
            aesGcm.Encrypt(iv, plaintext, ciphertext, tag);
        }

        byte[] result = new byte[iv.Length + ciphertext.Length + tag.Length];
        Buffer.BlockCopy(iv, 0, result, 0, iv.Length);
        Buffer.BlockCopy(ciphertext, 0, result, iv.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, iv.Length + ciphertext.Length, tag.Length);

        return Convert.ToBase64String(result);
    }
}
