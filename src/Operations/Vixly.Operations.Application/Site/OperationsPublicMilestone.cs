namespace Vixly.Operations.Application.Site;

// =========================================================
// FILE: OperationsPublicMilestone.cs
//
// PURPOSE:
// One public tracking milestone.
//
// RESPONSIBILITIES:
// - Carry the milestone code and when it was reached
//
// IMPORTANT NOTES:
// - Nothing else is ever public (ADR-0021)
// =========================================================
public sealed record OperationsPublicMilestone(string Code, DateTimeOffset ReachedUtc, string? LocationCode);
