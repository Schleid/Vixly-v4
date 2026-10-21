namespace Vixly.Operations.Application.UseCases.Tenancy.ResolveOperationsTenant;

// =========================================================
// FILE: ResolveOperationsTenantRequest.cs
//
// PURPOSE:
// Input of ResolveOperationsTenant.
//
// RESPONSIBILITIES:
// - Carry the slug taken from the host name
//
// IMPORTANT NOTES:
// - The slug comes from the host, never from a form
// =========================================================
public sealed class ResolveOperationsTenantRequest
{
    public string? Slug { get; init; }
}
