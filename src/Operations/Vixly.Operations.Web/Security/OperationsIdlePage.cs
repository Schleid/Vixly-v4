using System.Globalization;
using Microsoft.AspNetCore.Http;
using Vixly.Operations.Application.Identity;

namespace Vixly.Operations.Web.Security;

// =========================================================
// FILE: OperationsIdlePage.cs
//
// PURPOSE:
// Gives a staff page the company's inactivity times, as the seconds ops-session.js reads (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Read the times the sign-in check left on the request; the defaults otherwise
// - Give the warning and sign-out moments in seconds
//
// IMPORTANT NOTES:
// - Before 2026-10-27 the pages carried 780 and 900 seconds, fixed
// =========================================================
public static class OperationsIdlePage
{
    public const string Item = "vx.idle.times";

    public static OperationsIdleTimes Times(HttpContext? http) =>
        http?.Items[Item] as OperationsIdleTimes ?? OperationsIdleTimes.Default;

    public static string EndSeconds(HttpContext? http) =>
        ((int)Times(http).Staff.TotalSeconds).ToString(CultureInfo.InvariantCulture);

    public static string WarnSeconds(HttpContext? http)
    {
        OperationsIdleTimes times = Times(http);
        return ((int)(times.Staff - times.Warn).TotalSeconds).ToString(CultureInfo.InvariantCulture);
    }
}
