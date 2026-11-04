namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsSitePresets.cs
//
// PURPOSE:
// The default colours of each preset.
//
// RESPONSIBILITIES:
// - Give the primary and accent colours of a preset
//
// IMPORTANT NOTES:
// - Every preset passes the contrast rules
// =========================================================
public static class OperationsSitePresets
{
    public static (string Primary, string Accent) Colours(OperationsSitePreset preset) => preset switch
    {
        OperationsSitePreset.Harbour => ("#0F4C5C", "#E36414"),
        OperationsSitePreset.Sunrise => ("#B23A0B", "#B7791F"),
        OperationsSitePreset.Forest => ("#1E5631", "#A4580B"),
        OperationsSitePreset.Graphite => ("#1F2937", "#2563EB"),
        OperationsSitePreset.Ruby => ("#9F1239", "#C2410C"),
        _ => ("#1D4ED8", "#0E7490")
    };
}
