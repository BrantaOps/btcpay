using Branta;
using BTCPayServer.Plugins.Branta.Enums;
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

    public BrantaEnvironment Environment { get; set; }

    public InvoiceDataStatus Status { get; set; }

    public string FailureReason { get; set; }

    public DateTime ExpirationDate { get; set; }

    [Column("ZeroKnowledgeSecret")]
    public string Secret { get; set; }

    public string PluginVersion { get; set; }

    public string VerifyUrl { get; set; }

    public string GetVerifyLink()
    {
        if (ExpirationDate <= DateTime.UtcNow || Status != InvoiceDataStatus.Success)
        {
            return null;
        }

        return VerifyUrl;
    }
}
