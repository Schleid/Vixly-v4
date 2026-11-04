using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.AddToOperationsBundleList;

// =========================================================
// FILE: AddToOperationsBundleListUseCase.cs
//
// PURPOSE:
// Put parcels and closed repack boxes on a bundle's list from the computer, to be confirmed by a scan at the station (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage at the bundle's warehouse; open bundles only
// - What is on an open bundle's list goes on no other
// =========================================================
public sealed class AddToOperationsBundleListUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly OperationsBundleListing _listing;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AddToOperationsBundleListUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        OperationsBundleListing listing,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _listing = listing ?? throw new ArgumentNullException(nameof(listing));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AddToOperationsBundleListResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AddToOperationsBundleListRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage }, cancellationToken);

        if (refused is not null)
        {
            return AddToOperationsBundleListResponse.Failure(refused);
        }

        if (await _bundles.GetBundleAsync(request.BundleId, cancellationToken) is not { } bundle || warehouses.All(item => item.Id != bundle.WarehouseId))
        {
            return AddToOperationsBundleListResponse.Failure(OperationsBundleErrors.NotFound);
        }

        if (!bundle.IsOpen)
        {
            return AddToOperationsBundleListResponse.Failure(OperationsBundleErrors.NotOpen);
        }

        List<Guid> wanted = request.Ids.Distinct().Take(500).ToList();

        if (wanted.Count == 0)
        {
            return AddToOperationsBundleListResponse.Failure(OperationsBundleErrors.NothingChosen);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        Dictionary<Guid, OperationsBundleCandidate> candidates = (await _listing.CandidatesAsync(bundle, settings, cancellationToken)).ToDictionary(item => item.Id);
        IReadOnlyList<OperationsBundleItem> listed = await _bundles.ListItemsAsync(bundle.Id, cancellationToken);
        HashSet<Guid> already = listed.Select(item => item.ParcelId ?? item.BoxId!.Value).ToHashSet();
        wanted = wanted.Where(id => !already.Contains(id)).ToList();

        // Only what the bundle would take now: the same rules as a scan (a double click adds nothing more).
        if (wanted.Any(id => !candidates.ContainsKey(id)))
        {
            return AddToOperationsBundleListResponse.Failure(OperationsBundleErrors.NotAvailable);
        }

        // A list holds no more than a bundle does, so voiding it always removes every line (review of 94e28f4).
        if (listed.Count + wanted.Count > OperationsBundleLimits.MaxParcels)
        {
            return AddToOperationsBundleListResponse.Failure(OperationsBundleErrors.Full);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        foreach (OperationsBundleCandidate candidate in wanted.Select(id => candidates[id]))
        {
            _bundles.Add(OperationsBundleItem.Create(context.RequiredTenantId, bundle.Id, candidate.IsBox ? null : candidate.Id, candidate.IsBox ? candidate.Id : null, candidate.ShipmentId,
                access!.User.Id, utcNow));
        }

        bundle.ListChanged(utcNow);
        _audit.Record(access!.User.Id, "ops.bundles.listed", nameof(OperationsBundle), bundle.Id, after: new { bundle.Number, added = wanted.Count });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return AddToOperationsBundleListResponse.Success();
        }

        _unitOfWork.DiscardChanges();
        return AddToOperationsBundleListResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
