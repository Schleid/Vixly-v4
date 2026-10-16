using Vixly.Contracts.Platform.Common;

namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: PlatformSlugAvailability.cs
//
// PURPOSE:
// The answer to "can this portal slug be used?".
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Never says who holds a taken slug
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformSlugAvailability(bool Available, string? Slug, PlatformApiError? Reason);
