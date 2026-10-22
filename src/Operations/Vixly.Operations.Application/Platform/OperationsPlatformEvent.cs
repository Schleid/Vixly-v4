namespace Vixly.Operations.Application.Platform;

// =========================================================
// FILE: OperationsPlatformEvent.cs
//
// PURPOSE:
// One event read from Platform's feed.
//
// RESPONSIBILITIES:
// - Carry the sequence, type and tenant
//
// IMPORTANT NOTES:
// - The data stays in Infrastructure until a handler needs it
// =========================================================
public sealed record OperationsPlatformEvent(long Sequence, string Type, Guid? TenantId);
