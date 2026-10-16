using System.Globalization;
using System.Resources;
namespace Vixly.Shared.UI.Gallery.Resources;

// =========================================================
// FILE: WorkbenchLabels.cs
//
// PURPOSE:
// Resolve isolated component-demo text in en, fr and es.
//
// RESPONSIBILITIES:
// Select an allowlisted preview culture.
//
// IMPORTANT NOTES:
// Gallery only; never changes process culture or plane state.
// =========================================================
public static class WorkbenchLabels
{
    private static readonly ResourceManager Text = new("Vixly.Shared.UI.Gallery.Resources.WorkbenchText", typeof(WorkbenchLabels).Assembly);
    public static string Get(string key, string language) => Text.GetString(key, CultureInfo.GetCultureInfo(language is "fr" or "es" ? language : "en")) ?? key;
}
