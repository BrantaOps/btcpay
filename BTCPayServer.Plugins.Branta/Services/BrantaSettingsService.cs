using BTCPayServer.Data;
using BTCPayServer.Plugins.Branta.Interfaces;
using BTCPayServer.Plugins.Branta.Models;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Http;
using NBitcoin;
using NBXplorer;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Branta.Services;

public class BrantaSettingsService(IHttpContextAccessor httpContextAccessor, StoreRepository storeRepository) : IBrantaSettingsService
{
    private readonly string StoreBlobKey = "branta";

    public async Task<BrantaSettings> GetAsync(string storeId)
    {
        var store = await storeRepository.FindStore(storeId);

        var storeBlob = store.GetStoreBlob();

        if (storeBlob.AdditionalData.TryGetValue(StoreBlobKey, out var rawS))
        {
            if (rawS is JObject rawObj)
            {
                return new Serializer(null).ToObject<BrantaSettings>(rawObj);
            }
            else if (rawS.Type == JTokenType.String)
            {
                return new Serializer(null).ToObject<BrantaSettings>(rawS.Value<string>());
            }
        }

        return new BrantaSettings();
    }

    public async Task SetAsync(BrantaSettings settings)
    {
        var currentStore = httpContextAccessor.HttpContext.GetStoreData();

        var storeBlob = currentStore.GetStoreBlob();

        if (settings is null)
        {
            storeBlob.AdditionalData.Remove(StoreBlobKey);
        }
        else
        {
            storeBlob.AdditionalData.AddOrReplace(StoreBlobKey, new Serializer(null).ToString(settings));
        }

        if (currentStore.SetStoreBlob(storeBlob))
        {
            await storeRepository.UpdateStore(currentStore);
        }
    }
}
