using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Tenants;

namespace Vixly.Platform.Application.UseCases.Registrations.GetPlatformTenantRegistration;

// =========================================================
// FILE: GetPlatformTenantRegistrationUseCase.cs
//
// PURPOSE:
// Reads one registration for staff.
//
// RESPONSIBILITIES:
// - Check platform.tenants.view
// - Expire it first if a deadline has passed
// - Say whether the actor may resend or cancel
//
// IMPORTANT NOTES:
// - Hashes are never returned
// =========================================================
public sealed class GetPlatformTenantRegistrationUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformTenantRegistrationRepository _registrations;

    private readonly IPlatformTenantQueries _queries;

    private readonly PlatformTenantAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public GetPlatformTenantRegistrationUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformTenantRegistrationRepository registrations,
        IPlatformTenantQueries queries,
        PlatformTenantAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<GetPlatformTenantRegistrationResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformTenantRegistrationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.TenantsView, cancellationToken);

        if (await _registrations.GetAsync(request.RegistrationId, cancellationToken) is { } registration
            && registration.ExpireIfDue(_administration.UtcNow))
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        PlatformTenantRegistrationDetail? detail = await _queries.GetRegistrationAsync(request.RegistrationId, cancellationToken);

        if (detail is null)
        {
            return GetPlatformTenantRegistrationResponse.Failure(PlatformCommonErrors.NotFound);
        }

        PlatformTenantAbilities abilities = await _administration.ReadAbilitiesAsync(context, cancellationToken);

        return GetPlatformTenantRegistrationResponse.Success(detail, abilities.CanEdit);
    }
}
