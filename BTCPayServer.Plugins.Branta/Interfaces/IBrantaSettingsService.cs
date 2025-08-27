using BTCPayServer.Plugins.Branta.Models;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Branta.Interfaces;

public interface IBrantaSettingsService
{
    Task<BrantaSettings> GetAsync(string storeId);

    Task SetAsync(BrantaSettings settings);
}
