using Vixly.Contracts.Platform.Catalog;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Settings.SaveOperationsShippingServices;

// =========================================================
// FILE: SaveOperationsShippingServicesUseCase.cs
//
// PURPOSE:
// Saves which shipping services the tenant offers, and how (Slice 1E, D-O15).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SaveOperationsShippingServicesUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsShippingServiceRepository _services;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsShippingServicesUseCase(
        OperationsAccess access,
        IOperationsShippingServiceRepository services,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsShippingServicesResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsShippingServicesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ServicesManage, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsShippingServicesResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
        Dictionary<string, OperationsShippingServiceSetup> setups = (await _services.ListAsync(cancellationToken)).ToDictionary(setup => setup.ServiceCode, StringComparer.Ordinal);

        try
        {
            foreach (OperationsShippingServiceRow row in request.Services)
            {
                PlatformShippingServiceInfo service = PlatformShippingServices.Find(row.Code) ?? throw new DomainException(OperationsShippingServiceErrors.ServiceUnknown);

                if (!setups.TryGetValue(service.Code, out OperationsShippingServiceSetup? setup))
                {
                    setup = OperationsShippingServiceSetup.Create(context.RequiredTenantId, service.Code, utcNow);
                    setups[service.Code] = setup;
                    _services.Add(setup);
                }

                decimal? stored = service.DefaultVolumetricDivisor is null || row.VolumetricDivisor is not { } shown
                    ? null
                    : OperationsUnits.ToStoredDivisor(shown, settings.DimensionUnit, settings.WeightUnit);
                setup.Change(row.Offered, row.Label, row.MinDays, row.MaxDays, stored, service.DefaultVolumetricDivisor is not null, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsShippingServicesResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.services.changed", nameof(OperationsShippingServiceSetup), null,
            after: setups.Values.Where(setup => setup.Offered).Select(setup => new { code = setup.ServiceCode, setup.MinDays, setup.MaxDays, divisor = setup.VolumetricDivisor }).ToArray());

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsShippingServicesResponse.Success()
            : SaveOperationsShippingServicesResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
