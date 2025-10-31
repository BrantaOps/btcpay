using BTCPayServer.Plugins.Branta.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Branta.Classes;

public class BrantaClient(IHttpClientFactory httpClientFactory)
{
    public static readonly string PaymentVersion = "v2";

    private JsonSerializerSettings _jsonSettings = new()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver()
    };

    public async Task PostPaymentAsync(PaymentRequest paymentRequest, BrantaSettings brantaSettings)
    {

        var json = JsonConvert.SerializeObject(paymentRequest, _jsonSettings);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{PaymentVersion}/payments")
        {
            Content = content
        };

        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", brantaSettings.GetAPIKey());

        var httpClient = httpClientFactory.CreateClient();
        httpClient.BaseAddress = new Uri(brantaSettings.GetBrantaServerUrl());

        var response = await httpClient.SendAsync(request);

        if (response.StatusCode != HttpStatusCode.Created)
        {
            throw new BrantaPaymentException(response.StatusCode.ToString());
        }
    }
}

public class PaymentRequest
{
    public List<Destination> Destinations { get; set; }

    public string Description { get; set; }

    public string Ttl { get; set; }

    public string BtcPayServerPluginVersion { get; set; }
}

public class Destination
{
    public string Value { get; set; }

    public bool Zk { get; set; }
}
