using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registration.GetMyRegistrationStatus;

// =========================================================
// FILE: GetMyRegistrationStatusResponse.cs
//
// PURPOSE:
// Result of GetMyRegistrationStatus.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyRegistrationStatusResponse
{
    private GetMyRegistrationStatusResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantRegistrationStatus Status { get; private init; }

    public string MaskedEmail { get; private init; } = string.Empty;

    public string Slug { get; private init; } = string.Empty;

    public DateTimeOffset VerificationDeadlineUtc { get; private init; }

    public static GetMyRegistrationStatusResponse Success(PlatformTenantRegistrationStatus status, string maskedEmail, string slug, DateTimeOffset verificationDeadlineUtc)
    {
        return new GetMyRegistrationStatusResponse { Succeeded = true, Status = status, MaskedEmail = maskedEmail, Slug = slug, VerificationDeadlineUtc = verificationDeadlineUtc };
    }

    public static GetMyRegistrationStatusResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyRegistrationStatusResponse { Error = error };
    }
}
