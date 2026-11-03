using Vixly.Shared.Help;

namespace Vixly.Operations.Web.Features.Help;

// =========================================================
// FILE: OperationsHelpReaderContext.cs
//
// PURPOSE:
// Carries one request's trusted help visibility and brand values.
//
// RESPONSIBILITIES:
// - Keep tenant-specific values outside the shared catalogue.
//
// IMPORTANT NOTES:
// - Constructed from authenticated context and application reads.
// =========================================================
public sealed record OperationsHelpReaderContext(HelpReader Reader, string BrandName, string PortalAddress)
{
    public bool Available => !string.IsNullOrWhiteSpace(BrandName);
    public IReadOnlyDictionary<string, string> Tokens => new Dictionary<string, string>
        { ["brandName"] = BrandName, ["portalAddress"] = PortalAddress };
}
