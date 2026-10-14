using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.UpdatePlatformSalesSettings;

// =========================================================
// FILE: UpdatePlatformSalesSettingsResponse.cs
//
// PURPOSE:
// Changes the Sales settings: limits, offer validity, discount-ending notice, codes at registration (D-S22, D-S25).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformSalesSettingsResponse
{
    private UpdatePlatformSalesSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformSalesSettingsResponse Success()
    {
        return new UpdatePlatformSalesSettingsResponse { Succeeded = true };
    }

    public static UpdatePlatformSalesSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformSalesSettingsResponse { Error = error };
    }
}
