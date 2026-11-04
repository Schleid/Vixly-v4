using System.Globalization;
using Vixly.Operations.Domain.Delivery;

namespace Vixly.Operations.Web.Components.Delivery;

// =========================================================
// FILE: OperationsDeliveryWords.cs
//
// PURPOSE:
// Small wording helpers shared by the home-delivery pages (Slice 7 step 3).
//
// RESPONSIBILITIES:
// - Amounts with a dot, a route's status as a data value
//
// IMPORTANT NOTES:
// - No text here: words come from OperationsText
// =========================================================
public static class OperationsDeliveryWords
{
    public static string Amount(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture);

    public static string Status(OperationsRouteStatus status) => status.ToString().ToLowerInvariant();

    public static string Status(OperationsStopStatus status) => status.ToString().ToLowerInvariant();
}
