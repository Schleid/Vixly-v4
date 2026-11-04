namespace Vixly.Operations.Application.Setup;

// =========================================================
// FILE: OperationsOwnerInvitationRequested.cs
//
// PURPOSE:
// Outbox payload: invite the owner of a tenant (Slice 1, D-O3).
//
// RESPONSIBILITIES:
// - Carry the tenant id
//
// IMPORTANT NOTES:
// - Written by the Platform feed clock when an account is ready for setup
// =========================================================
public sealed record OperationsOwnerInvitationRequested(Guid TenantId);
