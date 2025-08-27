using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Plugins.Branta.Interfaces;
using BTCPayServer.Plugins.Branta.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Branta.Controllers;

[Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanModifyStoreSettings)]
public class UIBrantaController(IBrantaSettingsService brantaSettingsService, IInvoiceService invoiceService) : Controller
{
    private readonly IBrantaSettingsService _brantaSettingsService = brantaSettingsService;
    private readonly IInvoiceService _invoiceService = invoiceService;

    [HttpGet("stores/{storeId}/plugins/branta")]
    public async Task<IActionResult> EditBranta(string storeId)
    {
        return View(new BrantaSettingsViewModel()
        {
            Settings = await _brantaSettingsService.GetAsync(storeId)
        });
    }

    [HttpGet("stores/{storeId}/plugins/branta/logs")]
    public async Task<IActionResult> ViewLogs(string storeId, int page)
    {
        var result = await _invoiceService.GetAsync(storeId, 20, page);

        return View(result);
    }

    [HttpPost("stores/{storeId}/plugins/branta")]
    public async Task<IActionResult> BrantaSaveSettings(string storeId, BrantaSettingsViewModel brantaSettings)
    {
        try
        {
            await _brantaSettingsService.SetAsync(brantaSettings.Settings);

            TempData[WellKnownTempData.SuccessMessage] = "Settings saved!";
        }
        catch (Exception)
        {
            TempData[WellKnownTempData.ErrorMessage] = "Oops, an error occurred.";
        }

        return RedirectToAction(nameof(EditBranta), new { storeId });
    }
}
