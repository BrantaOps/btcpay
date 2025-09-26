using BTCPayServer.Plugins.Branta.Enums;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BTCPayServer.Plugins.Branta.Models;

public class BrantaSettingsViewModel
{
    public BrantaSettings Settings { get; set; }
}

public class BrantaSettings
{
    [Display(Name = "Staging API Key")]
    public string StagingApiKey { get; set; }

    [Display(Name = "API Key")]
    public string ProductionApiKey { get; set; }

    [Display(Name = "Enable Staging")]
    public bool StagingEnabled { get; set; } = false;

    [Display(Name = "Enable Branta")]
    public bool BrantaEnabled { get; set; } = false;

    [Display(Name = "Show Checkout Info on Verification Page")]
    public bool PostDescriptionEnabled { get; set; } = false;

    public string GetAPIKey()
    {
        return StagingEnabled ? StagingApiKey : ProductionApiKey;
    }

    public string GetBrantaServerUrl()
    {
        return GetBrantaServerUrl(StagingEnabled ? ServerEnvironment.Staging : ServerEnvironment.Production);
    }

    public static string GetBrantaServerUrl(ServerEnvironment environment)
    {
        if (Debugger.IsAttached)
        {
            return "http://localhost:3000";
        }

        return environment switch
        {
            ServerEnvironment.Staging => "https://staging.branta.pro",
            ServerEnvironment.Production => "https://guardrail.branta.pro",
            _ => null
        };
    }
}
