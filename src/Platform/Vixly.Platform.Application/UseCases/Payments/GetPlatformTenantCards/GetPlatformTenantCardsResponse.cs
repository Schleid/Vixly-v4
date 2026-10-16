using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformTenantCards;

// =========================================================
// FILE: GetPlatformTenantCardsResponse.cs
//
// PURPOSE:
// Reads a tenant's saved cards and automatic-charging consent (D-P3, D-P4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformTenantCardsResponse
{
    private GetPlatformTenantCardsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantCardsItem? Cards { get; private init; }

    public static GetPlatformTenantCardsResponse Success(PlatformTenantCardsItem? cards)
    {
        return new GetPlatformTenantCardsResponse { Succeeded = true, Cards = cards };
    }

    public static GetPlatformTenantCardsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformTenantCardsResponse { Error = error };
    }
}
