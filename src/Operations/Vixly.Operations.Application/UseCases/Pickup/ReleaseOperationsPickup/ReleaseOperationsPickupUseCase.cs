using Vixly.Operations.Application.Arrivals;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Billing;
using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Pickup.ReleaseOperationsPickup;

// =========================================================
// FILE: ReleaseOperationsPickupUseCase.cs
//
// PURPOSE:
// Release a shipment's parcels ready at the pickup counter, once paid and with the proof the company asks (Slice 7 step 2, R-5, R-6, R-9).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.pickup.release at the warehouse (R-9: every invoice paid first)
// - The full ID number is never kept: its kind and last four characters only (R-5)
// - One request key: a double click releases once
// =========================================================
public sealed class ReleaseOperationsPickupUseCase
{
    private readonly OperationsPickupDesk _desk;

    private readonly IOperationsPickupRepository _pickups;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsTracker _tracker;

    private readonly IOperationsFileStore _files;

    private readonly IOperationsImageInspector _images;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    private readonly OperationsPickupCodes _codes;

    public ReleaseOperationsPickupUseCase(
        OperationsPickupDesk desk,
        IOperationsPickupRepository pickups,
        IOperationsCustomerRepository customers,
        OperationsTracker tracker,
        IOperationsFileStore files,
        IOperationsImageInspector images,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        OperationsPickupCodes codes)
    {
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _pickups = pickups ?? throw new ArgumentNullException(nameof(pickups));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _images = images ?? throw new ArgumentNullException(nameof(images));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ReleaseOperationsPickupResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ReleaseOperationsPickupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return ReleaseOperationsPickupResponse.Failure(refused);
        }

        // A double click: the release the first click made (ADR-0019).
        if (request.RequestKey is { } key && await _pickups.FindByKeyAsync(key, cancellationToken) is { } made && made.ShipmentId == request.ShipmentId)
        {
            return ReleaseOperationsPickupResponse.Success(made.Id);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } here
            || await _desk.ShipmentAsync(request.ShipmentId, here, cancellationToken) is not { } view)
        {
            return ReleaseOperationsPickupResponse.Failure(OperationsPickupErrors.NotFound);
        }

        if (view.Ready.Count == 0)
        {
            return ReleaseOperationsPickupResponse.Failure(OperationsPickupErrors.NothingReady);
        }

        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        // Pay, then release (R-9): every invoice of the shipment paid or on account, and the storage owed billed first.
        if (OperationsBundling.Unpaid(view.Invoices))
        {
            return ReleaseOperationsPickupResponse.Failure(OperationsPickupErrors.PayFirst);
        }

        if (settings.DestinationStorage && view.StorageOwed(utcNow) > 0m)
        {
            return ReleaseOperationsPickupResponse.Failure(OperationsPickupErrors.BillFirst);
        }

        string? name = request.Collector switch
        {
            OperationsPickupCollector.Recipient => view.Shipment.RecipientName,
            OperationsPickupCollector.Named => view.Shipment.CollectorName,
            OperationsPickupCollector.Customer => await _customers.GetAsync(view.Shipment.CustomerId, cancellationToken) is { } customer ? customer.FirstName + " " + customer.LastName : null,
            _ => null
        };

        if (string.IsNullOrWhiteSpace(name))
        {
            return ReleaseOperationsPickupResponse.Failure(OperationsPickupErrors.CollectorInvalid);
        }

        OperationsPickupProofs proofs = settings.PickupProofs;

        if (proofs.HasFlag(OperationsPickupProofs.Id) && (request.IdKind is null || string.IsNullOrWhiteSpace(request.IdLast)))
        {
            return ReleaseOperationsPickupResponse.Failure(OperationsPickupErrors.IdRequired);
        }

        // A real PNG image (review), drawn on the counter's pad.
        byte[]? signature = request.Signature is { Length: > 60 and <= SignatureMaxBytes } drawn && drawn.AsSpan(0, 8).SequenceEqual(PngHeader)
            && _images.Inspect(drawn) is { ContentType: "image/png" } ? drawn : null;

        if ((proofs.HasFlag(OperationsPickupProofs.Signature) && signature is null) || (request.Signature is { Length: > 0 } && signature is null))
        {
            return ReleaseOperationsPickupResponse.Failure(OperationsPickupErrors.SignatureRequired);
        }

        OperationsImageInfo? photo = request.Photo is { Length: > 0 and <= PhotoMaxBytes } taken ? _images.Inspect(taken) : null;

        if ((proofs.HasFlag(OperationsPickupProofs.Photo) && photo is null) || (request.Photo is { Length: > 0 } && photo is null))
        {
            return ReleaseOperationsPickupResponse.Failure(OperationsPickupErrors.PhotoRequired);
        }

        bool codeChecked = false;

        if (proofs.HasFlag(OperationsPickupProofs.Code))
        {
            if (_codes.Current(view.Shipment) is null)
            {
                return ReleaseOperationsPickupResponse.Failure(view.Shipment.PickupCodeLocked ? OperationsPickupErrors.CodeLocked : OperationsPickupErrors.CodeNotSent);
            }

            // Every try is counted before the code is compared, in one statement (review): tries sent at once are all counted; five stop the code.
            if (!await _pickups.CountCodeTryAsync(view.Shipment, cancellationToken))
            {
                return ReleaseOperationsPickupResponse.Failure(OperationsPickupErrors.CodeLocked);
            }

            codeChecked = _codes.Matches(view.Shipment, request.Code);

            if (!codeChecked)
            {
                _audit.Record(access!.User.Id, "ops.pickup.code_failed", nameof(OperationsShipment), view.Shipment.Id, after: new { tries = view.Shipment.PickupCodeFailures });
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return ReleaseOperationsPickupResponse.Failure(view.Shipment.PickupCodeLocked ? OperationsPickupErrors.CodeLocked : OperationsPickupErrors.CodeWrong);
            }

            view.Shipment.PickupCodeAccepted(utcNow);
        }

        OperationsPickupRelease release;

        // Checked before any file is written.
        try
        {
            release = OperationsPickupRelease.Create(context.RequiredTenantId, view.Shipment.Id, here.Id, request.Collector, name, request.IdKind, request.IdLast, null, null, codeChecked,
                view.Ready.Count, access!.User.Id, request.RequestKey, utcNow);
        }
        catch (DomainException exception)
        {
            return ReleaseOperationsPickupResponse.Failure(exception.Error);
        }

        // The proof files are written first; whatever does not end in a saved release removes them (review).
        List<string> saved = new();
        bool committed = false;

        try
        {
            string? signatureKey = signature is null ? null : await _files.SaveAsync(context.RequiredTenantId, "pickups", signature, "image/png", cancellationToken);

            if (signatureKey is not null)
            {
                saved.Add(signatureKey);
            }

            string? photoKey = photo is null ? null : await _files.SaveAsync(context.RequiredTenantId, "pickups", request.Photo!, photo.ContentType, cancellationToken);

            if (photoKey is not null)
            {
                saved.Add(photoKey);
            }

            release = OperationsPickupRelease.Create(context.RequiredTenantId, view.Shipment.Id, here.Id, request.Collector, name, request.IdKind, request.IdLast, signatureKey, photoKey,
                codeChecked, view.Ready.Count, access!.User.Id, request.RequestKey, utcNow);
            _pickups.Add(release);

            if (await _tracker.RecordManyAsync(view.Ready.Select(parcel => (new OperationsTrackingDraft(OperationsTrackingSubject.Parcel, parcel.Id, OperationsTrackingCodes.PickedUp,
                new Dictionary<string, string>(StringComparer.Ordinal) { ["place"] = here.Name }, utcNow, "pickup:" + release.Id.ToString("N") + ":" + parcel.Id.ToString("N"),
                OperationsTrackingActor.Staff, OperationsTrackingSource.Counter) { ActorUserId = access.User.Id }, parcel)).ToList(), cancellationToken) is { } failed)
            {
                _unitOfWork.DiscardChanges();
                return ReleaseOperationsPickupResponse.Failure(failed);
            }

            // Collected: once every parcel of the shipment is finished.
            if (view.Parcels.All(Vixly.Operations.Application.Events.OperationsEventDesk.Finished))
            {
                view.Shipment.PickedUp(utcNow);
            }

            _audit.Record(access.User.Id, "ops.pickup.released", nameof(OperationsShipment), view.Shipment.Id, after: new
            {
                parcels = view.Ready.Count, collector = request.Collector.ToString(), id = request.IdKind?.ToString(), signed = signature is not null, photo = photo is not null, code = codeChecked
            });

            if (await _unitOfWork.SaveChangesAsync(cancellationToken))
            {
                committed = true;
                return ReleaseOperationsPickupResponse.Success(release.Id);
            }

            _unitOfWork.DiscardChanges();
            return request.RequestKey is { } again && await _pickups.FindByKeyAsync(again, cancellationToken) is { } other && other.ShipmentId == view.Shipment.Id
                ? ReleaseOperationsPickupResponse.Success(other.Id)
                : ReleaseOperationsPickupResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }
        finally
        {
            if (!committed)
            {
                foreach (string file in saved)
                {
                    await _files.DeleteAsync(context.RequiredTenantId, file, CancellationToken.None);
                }
            }
        }
    }

    private const int SignatureMaxBytes = 200 * 1024;

    private const int PhotoMaxBytes = 10 * 1024 * 1024;

    private static ReadOnlySpan<byte> PngHeader => new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
}
