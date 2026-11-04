using Vixly.Contracts.Platform.Common;

namespace Vixly.Public.Web.Platform;

// =========================================================
// FILE: PlatformApiResult.cs
//
// PURPOSE:
// The outcome of one Platform.Api call: a value, or Platform's error in the page language.
//
// RESPONSIBILITIES:
// - Carry the value or the error and the HTTP status
//
// IMPORTANT NOTES:
// - An unreachable Platform becomes the code public.platform_unavailable (S-9: no raw text)
// =========================================================
public sealed record PlatformApiResult<T>(T? Value, PlatformApiError? Error, int Status)
{
    public bool Succeeded => Error is null;
}
