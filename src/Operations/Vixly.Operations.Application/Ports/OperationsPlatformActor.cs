namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsPlatformActor.cs
//
// PURPOSE:
// The staff member Operations acts for when it calls Platform (ADR-0003: Platform holds no tenant users).
//
// RESPONSIBILITIES:
// - Carry the user reference, name, role, step-up and request details for the service token
//
// IMPORTANT NOTES:
// - Platform checks the role and step-up itself (Slice 7, D-K8)
// - Country: where the person is (owner, 2026-10-21), sent with a device registration.
// =========================================================
public sealed record OperationsPlatformActor(Guid TenantId, Guid UserId, string Name, string Role, bool StepUp, string? ClientIp, string? UserAgent, string? Country = null);
