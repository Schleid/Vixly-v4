namespace Vixly.Operations.Api.Internal;

// =========================================================
// FILE: OperationsInternalEndpointMetadata.cs
//
// PURPOSE:
// The scope an internal endpoint needs and whether the token must name a tenant.
//
// RESPONSIBILITIES:
// - Mark internal endpoint groups
//
// IMPORTANT NOTES:
// - Read by OperationsInternalAuthentication
// =========================================================
public sealed record OperationsInternalEndpointMetadata(string Scope, bool TenantRequired);
