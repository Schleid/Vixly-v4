using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Tenants;

namespace Vixly.Platform.Application.UseCases.Registration.CheckPortalSlugAvailability;

// =========================================================
// FILE: CheckPortalSlugAvailabilityUseCase.cs
//
// PURPOSE:
// Tells the public form whether a portal slug can be used.
//
// RESPONSIBILITIES:
// - Normalise the slug and refuse invalid or reserved names
// - Say whether it is free
//
// IMPORTANT NOTES:
// - Anonymous and rate-limited (S-6)
// - Answers yes or a reason code only: never who holds it
// =========================================================
public sealed class CheckPortalSlugAvailabilityUseCase
{
    private readonly PlatformPortalSlugs _slugs;

    private readonly TimeProvider _timeProvider;

    public CheckPortalSlugAvailabilityUseCase(
        PlatformPortalSlugs slugs,
        TimeProvider timeProvider)
    {
        _slugs = slugs ?? throw new ArgumentNullException(nameof(slugs));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CheckPortalSlugAvailabilityResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        CheckPortalSlugAvailabilityRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSlugCheck check = await _slugs.CheckAsync(request.Slug, _timeProvider.GetUtcNow(), cancellationToken);

        return check.Error is null
            ? CheckPortalSlugAvailabilityResponse.Success(check.Slug!)
            : CheckPortalSlugAvailabilityResponse.Failure(check.Error);
    }
}
