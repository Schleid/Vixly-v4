using Vixly.Contracts.Platform.Catalog;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Settings.ListOperationsShippingServices;

// =========================================================
// FILE: ListOperationsShippingServicesUseCase.cs
//
// PURPOSE:
// Lists Vixly's shipping services with the tenant's setup of each (Slice 1E, D-O15).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListOperationsShippingServicesUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsShippingServiceRepository _services;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public ListOperationsShippingServicesUseCase(
        OperationsAccess access,
        IOperationsShippingServiceRepository services,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListOperationsShippingServicesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsShippingServicesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ServicesView, false, cancellationToken);

        if (refused is not null)
        {
            return ListOperationsShippingServicesResponse.Failure(refused);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        Dictionary<string, OperationsShippingServiceSetup> setups = (await _services.ListAsync(cancellationToken)).ToDictionary(setup => setup.ServiceCode, StringComparer.Ordinal);

        List<OperationsShippingServiceRow> rows = PlatformShippingServices.All.Select(service => setups.TryGetValue(service.Code, out OperationsShippingServiceSetup? setup)
            ? new OperationsShippingServiceRow(service.Code, service.Mode, setup.Offered, setup.Label, setup.MinDays, setup.MaxDays,
                setup.VolumetricDivisor is { } stored ? OperationsUnits.ToShownDivisor(stored, settings.DimensionUnit, settings.WeightUnit) : null, service.DefaultVolumetricDivisor is not null)
            : new OperationsShippingServiceRow(service.Code, service.Mode, false, null, service.DefaultMinDays, service.DefaultMaxDays,
                service.DefaultVolumetricDivisor is { } divisor ? OperationsUnits.ToShownDivisor(divisor, settings.DimensionUnit, settings.WeightUnit) : null, service.DefaultVolumetricDivisor is not null))
            .ToList();

        string unit = (settings.DimensionUnit == OperationsDimensionUnit.Inch ? "in3" : "cm3") + "/" + (settings.WeightUnit == OperationsWeightUnit.Pound ? "lb" : "kg");
        return ListOperationsShippingServicesResponse.Success(rows, unit, access!.HasTenantWide(OperationsPermissions.ServicesManage));
    }
}
