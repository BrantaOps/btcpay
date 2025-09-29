using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Branta.Classes;

public enum TTLOptions
{
    [Display(Name = "30 Minutes")]
    ThirtyMinutes = 30,

    [Display(Name = "4 hours")]
    FourHours = 240,

    [Display(Name = "1 day")]
    OneDay = 1440,

    [Display(Name = "7 days")]
    SevenDays = 10080
}