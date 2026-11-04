namespace Vixly.Operations.Application.Platform;

// =========================================================
// FILE: OperationsPlatformEventPage.cs
//
// PURPOSE:
// One page of Platform's event feed.
//
// RESPONSIBILITIES:
// - Carry the events, the next position and whether more wait
//
// IMPORTANT NOTES:
// - Read in order; the position only moves forward
// =========================================================
public sealed record OperationsPlatformEventPage(IReadOnlyList<OperationsPlatformEvent> Items, long Next, bool More);
