using BTCPayServer.Plugins.Branta.Data.Domain;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Branta.Models;

public class InvoiceDto : InvoiceData
{
    public string VerifyLink { get; set; }

    public InvoiceDto(InvoiceData invoice)
    {
        Id = invoice.Id;
        InvoiceId = invoice.InvoiceId;
        StoreId = invoice.StoreId;
        PaymentId = invoice.PaymentId;
        DateCreated = invoice.DateCreated;
        ProcessingTime = invoice.ProcessingTime;
        Environment = invoice.Environment;
        Status = invoice.Status;
        FailureReason = invoice.FailureReason;
        ExpirationDate = invoice.ExpirationDate;
        VerifyLink = invoice.GetVerifyLink();
    }
}

public class InvoiceDataViewModel
{
    public List<InvoiceDto> Invoices { get; set; }

    public int CurrentPage { get; set; }

    public int TotalPages { get; set; }

    public string StoreId { get; set; }
}
