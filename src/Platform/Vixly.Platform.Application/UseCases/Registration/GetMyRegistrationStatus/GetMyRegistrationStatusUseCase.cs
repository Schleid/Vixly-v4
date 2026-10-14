using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.UseCases.Registration.GetMyRegistrationStatus;

// =========================================================
// FILE: GetMyRegistrationStatusUseCase.cs
//
// PURPOSE:
// Tells the registrant what to do next.
//
// RESPONSIBILITIES:
// - Open the registration from its access token
// - Return its step, the masked email and the link deadline
//
// IMPORTANT NOTES:
// - Anonymous and rate-limited (S-6)
// - Never returns the stored company or owner details
// =========================================================
public sealed class GetMyRegistrationStatusUseCase
{
    private readonly PlatformRegistrationLinks _links;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public GetMyRegistrationStatusUseCase(
        PlatformRegistrationLinks links,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetMyRegistrationStatusResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        GetMyRegistrationStatusRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformRegistrationAccess access = await _links.OpenAsync(request.AccessToken, _timeProvider.GetUtcNow(), cancellationToken);

        if (access.Registration is not PlatformTenantRegistration registration)
        {
            if (access.ExpiredNow)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return GetMyRegistrationStatusResponse.Failure(access.Error!);
        }

        return GetMyRegistrationStatusResponse.Success(
            registration.Status,
            Mask(registration.OwnerEmail.Value),
            registration.Slug,
            registration.VerificationDeadlineUtc);
    }

    private static string Mask(string email)
    {
        int at = email.IndexOf('@', StringComparison.Ordinal);

        return at <= 1 ? "*" + email[Math.Max(at, 0)..] : email[0] + new string('*', at - 1) + email[at..];
    }
}
