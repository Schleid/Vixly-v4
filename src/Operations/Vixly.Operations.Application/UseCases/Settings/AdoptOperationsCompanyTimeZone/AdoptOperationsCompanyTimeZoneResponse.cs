using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Settings.AdoptOperationsCompanyTimeZone;

// =========================================================
// FILE: AdoptOperationsCompanyTimeZoneResponse.cs
//
// PURPOSE:
// Whether the company time zone was copied into the Operations settings.
//
// RESPONSIBILITIES:
// - Carry the result of AdoptOperationsCompanyTimeZoneUseCase
//
// IMPORTANT NOTES:
// - Nothing to copy is a success with Copied false
// =========================================================
public sealed class AdoptOperationsCompanyTimeZoneResponse
{
    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool Copied { get; private init; }

    public static AdoptOperationsCompanyTimeZoneResponse Success(bool copied)
    {
        return new AdoptOperationsCompanyTimeZoneResponse { Succeeded = true, Copied = copied };
    }

    public static AdoptOperationsCompanyTimeZoneResponse Failure(Error error)
    {
        return new AdoptOperationsCompanyTimeZoneResponse { Succeeded = false, Error = error };
    }
}
