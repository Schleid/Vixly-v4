using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.GetPlatformSalesSettings;

// =========================================================
// FILE: GetPlatformSalesSettingsResponse.cs
//
// PURPOSE:
// Reads the Sales settings (D-S22).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformSalesSettingsResponse
{
    private GetPlatformSalesSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformSalesSettingsItem? Settings { get; private init; }

    public bool CanEdit { get; private init; }

    public static GetPlatformSalesSettingsResponse Success(PlatformSalesSettingsItem? settings, bool canEdit)
    {
        return new GetPlatformSalesSettingsResponse { Succeeded = true, Settings = settings, CanEdit = canEdit };
    }

    public static GetPlatformSalesSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformSalesSettingsResponse { Error = error };
    }
}
