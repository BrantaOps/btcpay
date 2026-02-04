using Branta.Enums;
using BTCPayServer.Plugins.Branta.Enums;
using BTCPayServer.Plugins.Branta.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace BTCPayServer.Plugins.Branta.Data.Domain;

public class InvoiceData
{
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public string Id { get; set; }

    public string InvoiceId { get; set; }

    public string StoreId { get; set; }

    public string PaymentId { get; set; }

    public DateTime DateCreated { get; set; }

    public int ProcessingTime { get; set; }

    public BrantaServerBaseUrl Environment { get; set; }

    public InvoiceDataStatus Status { get; set; }

    public string FailureReason { get; set; }

    public DateTime ExpirationDate { get; set; }

    public string ZeroKnowledgeSecret { get; set; }

    public string PluginVersion { get; set; }

    public string GetVerifyLink(BrantaSettings brantaSettings)
    {
        if (ExpirationDate <= DateTime.UtcNow || Status != InvoiceDataStatus.Success)
        {
            return null;
        }

        var baseUrl = BrantaSettings.GetBrantaServerUrl(Environment);

        var path = ZeroKnowledgeSecret != null && !brantaSettings.EnableV3Verify ? "zk-verify" : "verify";
        var secret = ZeroKnowledgeSecret != null ? $"#secret={ZeroKnowledgeSecret}" : "";

        var version = brantaSettings.EnableV3Verify ? "v3" : "v2";

        return $"{baseUrl}/{version}/{path}/{Uri.EscapeDataString(PaymentId)}{secret}";
    }
}
