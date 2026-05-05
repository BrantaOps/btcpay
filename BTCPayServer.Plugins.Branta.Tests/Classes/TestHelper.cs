using System.Text.RegularExpressions;
using System.Web;

namespace BTCPayServer.Plugins.Branta.Tests.Classes;

public class TestHelper
{
    public static string? GetValueFromZeroKnowledgeUrl(string url)
    {
        var match = Regex.Match(new Uri(url).AbsolutePath, @"/verify/(.+)$");

        if (!match.Success)
            return null;

        var encodedValue = match.Groups[1].Value;
        return HttpUtility.UrlDecode(encodedValue);
    }
}
