namespace Vixly.Contracts.Platform.Events;

// =========================================================
// FILE: PlatformRegistrationAccountSetupReadyEvent.cs
//
// PURPOSE:
// A registration is ready for the owner to set up the account (Operations Slice 1).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Ids only; Operations reads the owner contact with GET /tenant
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformRegistrationAccountSetupReadyEvent(Guid TenantId, Guid RegistrationId, DateTimeOffset OccurredUtc);
