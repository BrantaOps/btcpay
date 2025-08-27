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

    [Display(Name = "Enable Production")]
    public bool ProductionEnabled { get; set; } = false;

    [Display(Name = "Enable Branta")]
    public bool BrantaEnabled { get; set; } = false;

    public static string GetBrantaServerUrl(ServerEnvironment environment = ServerEnvironment.Production)
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
