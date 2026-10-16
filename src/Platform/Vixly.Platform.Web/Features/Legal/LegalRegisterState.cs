using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Legal;

// =========================================================
// FILE: LegalRegisterState.cs
//
// PURPOSE:
// What the acceptance register shows.
//
// RESPONSIBILITIES:
// - Hold the required version and one page of tenants
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record LegalRegisterState(PlatformLegalDocumentItem? Required, IReadOnlyList<PlatformLegalRegisterItem> Items, int Page, int TotalPages, int TotalCount, bool Loaded, Error? Error)
{
    public static LegalRegisterState Initial { get; } = new(null, Array.Empty<PlatformLegalRegisterItem>(), 1, 0, 0, false, null);
}
