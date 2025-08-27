namespace BTCPayServer.Plugins.Branta.Classes;

public static class Helper
{
    public static string GetVersion()
    {
        return typeof(BrantaPlugin).Assembly.GetName().Version?.ToString();
    }
}
