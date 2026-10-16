using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Collections.GetPlatformCollectionsSettings;

// =========================================================
// FILE: GetPlatformCollectionsSettingsResponse.cs
//
// PURPOSE:
// Reads the collections settings (D-C12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformCollectionsSettingsResponse
{
    private GetPlatformCollectionsSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformCollectionsSettingsItem? Settings { get; private init; }

    public static GetPlatformCollectionsSettingsResponse Success(PlatformCollectionsSettingsItem? settings)
    {
        return new GetPlatformCollectionsSettingsResponse { Succeeded = true, Settings = settings };
    }

    public static GetPlatformCollectionsSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformCollectionsSettingsResponse { Error = error };
    }
}
