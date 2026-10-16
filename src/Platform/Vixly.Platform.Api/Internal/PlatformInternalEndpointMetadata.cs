namespace Vixly.Platform.Api.Internal;

// =========================================================
// FILE: PlatformInternalEndpointMetadata.cs
//
// PURPOSE:
// Marks an internal route with the scope it needs and whether the token must name a tenant.
//
// RESPONSIBILITIES:
// - Carry the route rule for the internal authentication step
//
// IMPORTANT NOTES:
// - A route under /internal without this marker answers 404
// =========================================================
public sealed record PlatformInternalEndpointMetadata(string Scope, bool TenantRequired);
