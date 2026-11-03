using Microsoft.Extensions.Localization;
using Vixly.Contracts.Operations.Packages;
using Vixly.Customer.Resources;
using Vixly.Shared.Localization;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerDriverWords.cs
//
// PURPOSE:
// The words about the driver bringing a shipment (Slice 12C, owner H-5), the same on the page and in its refresh.
//
// RESPONSIBILITIES:
// - Say delivered, next, or how many stops before; give the place when there is one
//
// IMPORTANT NOTES:
// - Text from CustomerText (L-10); never "Vixly"
// =========================================================
public static class CustomerDriverWords
{
    public static string Line(CustomerDriverDto driver, IStringLocalizer<CustomerText> text)
    {
        ArgumentNullException.ThrowIfNull(driver);
        ArgumentNullException.ThrowIfNull(text);

        return driver.Delivered ? text["ui.driver.delivered"].Value
            : driver.StopsAway == 0 ? text["ui.driver.next"].Value
            : VixlyText.Format(text, "ui.driver.stops_away", "count", driver.StopsAway);
    }

    public static object Json(CustomerDriverDto driver, IStringLocalizer<CustomerText> text) => new
    {
        line = Line(driver, text),
        lat = driver.Latitude,
        lng = driver.Longitude,
        at = driver.TakenUtc,
        attribution = driver.MapAttribution
    };
}
