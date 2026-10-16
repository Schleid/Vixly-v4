using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Collections;

// =========================================================
// FILE: CollectionCaseState.cs
//
// PURPOSE:
// What the collections case page shows.
//
// RESPONSIBILITIES:
// - Hold the case, invoices, history, abilities, step-up and messages
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record CollectionCaseState(PlatformCollectionCaseDetail? Detail, bool Loaded, bool StepUpPending, string? DoneKey, Error? Error)
{
    public static CollectionCaseState Initial { get; } = new(null, false, false, null, null);
}
