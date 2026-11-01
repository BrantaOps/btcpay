using Branta;
using BTCPayServer.Plugins.Branta.Classes;
using System;
using System.ComponentModel.DataAnnotations;

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

    public string GetAPIKey()
    {
        return StagingEnabled ? StagingApiKey : ProductionApiKey;
    }

    public BrantaEnvironment GetEnvironment()
    {
#if DEBUG
        return BrantaEnvironment.Localhost;
#else
        return StagingEnabled ? BrantaEnvironment.Staging : BrantaEnvironment.Production;
#endif
    }

    public static string GetBrantaServerUrl(BrantaEnvironment environment)
    {
#if DEBUG
        environment = BrantaEnvironment.Localhost;
#endif
        return environment switch
        {
            BrantaEnvironment.Staging => "https://staging.guardrail.branta.pro",
            BrantaEnvironment.Production => "https://guardrail.branta.pro",
            BrantaEnvironment.Localhost => "http://localhost:3000",
            _ => throw new ArgumentOutOfRangeException(nameof(environment), environment, null)
        };
    }
}
