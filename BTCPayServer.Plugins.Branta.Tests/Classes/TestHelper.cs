using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace BTCPayServer.Plugins.Branta.Tests.Classes;

public class TestHelper
{

    public static string Decrypt(string encryptedValue, string secret)
    {
        byte[] keyData;
        using (var sha256 = SHA256.Create())
        {
            keyData = sha256.ComputeHash(Encoding.UTF8.GetBytes(secret));
        }

        byte[] fullData = Convert.FromBase64String(encryptedValue);

        byte[] iv = new byte[12];
        Buffer.BlockCopy(fullData, 0, iv, 0, 12);

        byte[] tag = new byte[16];
        Buffer.BlockCopy(fullData, fullData.Length - 16, tag, 0, 16);

        byte[] ciphertext = new byte[fullData.Length - 12 - 16];
        Buffer.BlockCopy(fullData, 12, ciphertext, 0, ciphertext.Length);

        byte[] plaintext = new byte[ciphertext.Length];

        using (AesGcm aesGcm = new(keyData, 16))
        {
            aesGcm.Decrypt(iv, ciphertext, tag, plaintext);
        }

        return Encoding.UTF8.GetString(plaintext);
    }

    public static string GetSecret(string url)
    {
        return new Uri(url).Fragment.TrimStart('#').Substring("secret=".Length);
    }

    public static string? GetValueFromZeroKnowledgeUrl(string url)
    {
        var match = Regex.Match(new Uri(url).AbsolutePath, @"/zk-verify/(.+)$");

        if (!match.Success)
            return null;

        var encodedValue = match.Groups[1].Value;
        return HttpUtility.UrlDecode(encodedValue);
    }
}
