using BTCPayServer.Plugins.Branta.Models;
using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Branta.Classes;

public class BrantaClient(IHttpClientFactory httpClientFactory)
{
    public static readonly string PaymentVersion = "v1";

    public async Task PostPaymentAsync(PaymentRequest paymentRequest, BrantaSettings brantaSettings)
    {
        var json = JsonConvert.SerializeObject(paymentRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        content.Headers.Add("Authorization", $"Bearer {brantaSettings.ProductionApiKey}");

        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(BrantaSettings.GetBrantaServerUrl());

        var response = await httpClient.PostAsync($"{PaymentVersion}/payments", content);

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new BrantaPaymentException(response.StatusCode.ToString());
        }
    }
}

public class PaymentRequest
{
    public Payment payment { get; set; }
}

public class Payment
{
    public string description { get; set; }
    public string payment { get; set; }
    public string[] alt_payments { get; set; }
    public string ttl { get; set; }
    public string btcPayServerPluginVersion { get; set; }
    public long value { get; set; }
}
