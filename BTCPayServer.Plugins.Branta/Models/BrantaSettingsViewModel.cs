using Branta.Enums;
using Branta.Extensions;
using BTCPayServer.Plugins.Branta.Classes;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace BTCPayServer.Plugins.Branta.Models;

public class BrantaSettingsViewModel
{
    public string StoreId { get; set; }

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

    [Display(Name = "Allow Guardrail Verification for")]
    public int TTL { get; set; } = (int)TTLOptions.ThirtyMinutes;

    [Display(Name = "Show Verify Link at Checkout")]
    public bool ShowVerifyLink { get; set; } = true;

    [Display(Name = "Enable Zero-Knowledge")]
    public bool EnableZeroKnowledge { get; set; } = false;

    public string GetAPIKey()
    {
        return StagingEnabled ? StagingApiKey : ProductionApiKey;
    }

    public BrantaServerBaseUrl GetBrantaServerUrl()
    {
        if (Debugger.IsAttached)
        {
            return BrantaServerBaseUrl.Localhost;
        }

        return StagingEnabled ? BrantaServerBaseUrl.Staging : BrantaServerBaseUrl.Production;
    }

    public static string GetBrantaServerUrl(BrantaServerBaseUrl environment)
    {
        if (Debugger.IsAttached)
        {
            return BrantaServerBaseUrl.Localhost.GetUrl();
        }

        return environment.GetUrl();
    }
}
