namespace Vixly.Contracts.Platform.Internal.Subscription;

// =========================================================
// FILE: PlatformSubscriptionChangeBody.cs
//
// PURPOSE:
// The plan, term and licences the tenant wants (call 21).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Term: monthly or annual
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformSubscriptionChangeBody(Guid PlanId, string Term, int Licences);
