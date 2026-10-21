using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupplySettings;

// =========================================================
// FILE: SaveOperationsSupplySettingsUseCase.cs
//
// PURPOSE:
// The company's supplies stock settings (Slice 5E, S-9): count the stock or not, and what the counter does when an item has none left.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.charges.configure, as the supplies catalogue
// =========================================================
public sealed class SaveOperationsSupplySettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsSupplySettingsUseCase(
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsSupplySettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsSupplySettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ChargesConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsSupplySettingsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        object before = new
        {
            count = settings.CountSupplyStock, stockOut = settings.SupplyStockOut.ToString(), discounts = settings.SupplyDiscountsAllowed,
            line = settings.SupplyDiscountLinePercent, order = settings.SupplyDiscountOrderPercent, reason = settings.SupplyDiscountReasonRequired
        };

        try
        {
            settings.ChangeSupplyStock(request.CountStock, request.StockOut, utcNow);
            settings.ChangeSupplyDiscounts(request.DiscountsAllowed, request.DiscountLinePercent, request.DiscountOrderPercent, request.DiscountReasonRequired, utcNow);
        }
        catch (DomainException exception)
        {
            return SaveOperationsSupplySettingsResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.supplies.settings_saved", nameof(OperationsTenantSettings), settings.Id,
            before: before, after: new
            {
                count = settings.CountSupplyStock, stockOut = settings.SupplyStockOut.ToString(), discounts = settings.SupplyDiscountsAllowed,
                line = settings.SupplyDiscountLinePercent, order = settings.SupplyDiscountOrderPercent, reason = settings.SupplyDiscountReasonRequired
            });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsSupplySettingsResponse.Success()
            : SaveOperationsSupplySettingsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
