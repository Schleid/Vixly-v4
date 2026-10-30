using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.UseCases.Settings.GetOperationsSetupChecklist;

// =========================================================
// FILE: GetOperationsSetupChecklistUseCase.cs
//
// PURPOSE:
// Works out which setup steps the tenant has done (Slice 1E, design section 3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetOperationsSetupChecklistUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsShippingServiceRepository _services;

    public GetOperationsSetupChecklistUseCase(
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        IOperationsLocationRepository locations,
        IOperationsBrandRepository brands,
        IOperationsUserRepository users,
        IOperationsShippingServiceRepository services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _users = users ?? throw new ArgumentNullException(nameof(users));
    }

    public async Task<GetOperationsSetupChecklistResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsSetupChecklistRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // The checklist is for the Owner and Administrators; others get an empty list.
        if (await _access.LoadAsync(context, cancellationToken) is not { } access || (!access.IsOwner && !access.IsAdministrator))
        {
            return GetOperationsSetupChecklistResponse.Success(Array.Empty<OperationsSetupItem>());
        }

        IReadOnlyList<OperationsBranch> branches = await _locations.ListBranchesAsync(cancellationToken);
        IReadOnlyList<OperationsWarehouse> warehouses = await _locations.ListWarehousesAsync(cancellationToken);
        OperationsTenantBrand? brand = await _brands.FindAsync(cancellationToken);
        (IReadOnlyList<OperationsUser> _, int staff) = await _users.ListAsync(null, 1, 1, cancellationToken);

        return GetOperationsSetupChecklistResponse.Success(new[]
        {
            new OperationsSetupItem("settings", await _settings.FindAsync(cancellationToken) is not null, "/staff/settings"),
            new OperationsSetupItem("branch", branches.Any(branch => branch.Status == OperationsLocationStatus.Active), "/staff/locations"),
            new OperationsSetupItem("receiving_warehouse", warehouses.Any(warehouse => warehouse.Status == OperationsLocationStatus.Active && warehouse.Roles.HasFlag(OperationsWarehouseRoles.Receiving)), "/staff/locations"),
            new OperationsSetupItem("services", (await _services.ListAsync(cancellationToken)).Any(setup => setup.Offered), "/staff/services"),
            new OperationsSetupItem("site", brand is { PublishedVersion: > 0 }, "/staff/site"),
            new OperationsSetupItem("staff", staff > 1, "/staff/people")
        });
    }
}
