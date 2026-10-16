namespace Vixly.Platform.Domain.Collections;

// =========================================================
// FILE: PlatformCollectionExtension.cs
//
// PURPOSE:
// The result of granting an extension: the history line and whether a block was lifted.
//
// RESPONSIBILITIES:
// - Tell the caller to lift the subscription's block when the case was blocked
//
// IMPORTANT NOTES:
// - The subscription is changed by the caller in the same commit
// =========================================================
public sealed record PlatformCollectionExtension(PlatformCollectionCaseEvent Event, bool LiftedBlock);
