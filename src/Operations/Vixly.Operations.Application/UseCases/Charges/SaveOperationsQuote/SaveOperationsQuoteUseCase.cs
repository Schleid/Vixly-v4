using Vixly.Contracts.Platform.Catalog;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsQuote;

// =========================================================
// FILE: SaveOperationsQuoteUseCase.cs
//
// PURPOSE:
// Saves a quote for a customer on one service on a route, with an optional preferred price (Slice 4; routes, Slice 4C).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SaveOperationsQuoteUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsRater _rater;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsChargeRepository _charges;

    private readonly OperationsNumbering _numbering;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public SaveOperationsQuoteUseCase(
        OperationsAccess access,
        OperationsRater rater,
        IOperationsCustomerRepository customers,
        IOperationsChargeRepository charges,
        OperationsNumbering numbering,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _rater = rater ?? throw new ArgumentNullException(nameof(rater));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SaveOperationsQuoteResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsQuoteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.QuotesCreate, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsQuoteResponse.Failure(refused);
        }

        if (!Vixly.Operations.Application.Receiving.OperationsCommandKey.IsValid(request.CommandKey))
        {
            return SaveOperationsQuoteResponse.Failure(Vixly.Operations.Domain.Tracking.OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        string key = "qt:" + access!.User.Id.ToString("N")[..8] + ":" + request.CommandKey;

        // Sent twice (a double click): the quote the first one saved.
        if (await _charges.FindQuoteByKeyAsync(key, cancellationToken) is { } earlier)
        {
            return SaveOperationsQuoteResponse.Success(earlier.Id, earlier.Number);
        }

        if (string.IsNullOrWhiteSpace(request.CustomerCode) || await _customers.FindByCodeAsync(request.CustomerCode.Trim().ToUpperInvariant(), cancellationToken) is not { } customer)
        {
            return SaveOperationsQuoteResponse.Failure(OperationsChargeErrors.CustomerNotFound);
        }

        OperationsRatingSetup setup = await _rater.LoadAsync(context.RequiredTenantId, customer.Id, cancellationToken);
        DateTimeOffset utcNow = _rater.UtcNow;

        if (setup.Pieces(request.Pieces) is not { } pieces)
        {
            return SaveOperationsQuoteResponse.Failure(OperationsChargeErrors.PiecesInvalid);
        }

        if (!setup.TryRoute(request.OriginWarehouseId, request.DestinationWarehouseId, out OperationsWarehouse? from, out OperationsWarehouse to))
        {
            return SaveOperationsQuoteResponse.Failure(OperationsChargeErrors.RouteInvalid);
        }

        if (request.Delivery != OperationsDeliveryModes.None && !to.Offers(request.Delivery))
        {
            return SaveOperationsQuoteResponse.Failure(Vixly.Operations.Domain.Shipments.OperationsShipmentErrors.DeliveryModeUnavailable);
        }

        if (string.IsNullOrWhiteSpace(request.ServiceCode) || !setup.Offers(request.ServiceCode.Trim())
            || setup.Rate(request.ServiceCode.Trim(), from, to, customer.Id, pieces, request.Consolidated, request.Insured, utcNow, request.Delivery, request.ExtraQuantities) is not { } rating)
        {
            return SaveOperationsQuoteResponse.Failure(OperationsChargeErrors.NotPriced);
        }

        // Insured on paper only would mislead the customer: the value must be insurable.
        if (rating.InsuranceRefused)
        {
            return SaveOperationsQuoteResponse.Failure(OperationsChargeErrors.InsuranceRefused);
        }

        OperationsQuote quote;

        try
        {
            quote = OperationsQuote.Create(context.RequiredTenantId, await _numbering.NextAsync(OperationsNumberKind.Quote, cancellationToken), customer.Id, rating.ServiceCode, to.Address.CountryCode,
                request.Consolidated, request.Insured,
                pieces.Select(piece => new OperationsQuotePiece(piece.WeightKg, piece.LengthCm, piece.WidthCm, piece.HeightCm, piece.DeclaredValue,
                    setup.ItemTypes.FirstOrDefault(type => type.Id == piece.ItemTypeId)?.Name, piece.Contents, piece.ItemTypeId)).ToList(),
                rating.Lines.Select(line => new OperationsQuoteLine(line.Kind, line.Label, line.Piece, line.Quantity, line.Basis, line.UnitPrice, line.Amount, line.IsGovernmentFee)).ToList(),
                setup.Settings.CurrencyCode, request.PreferredTotal, request.Reason, request.ValidDays, access.User.Id, utcNow, key, from?.Id, to.Id);
        }
        catch (DomainException exception)
        {
            return SaveOperationsQuoteResponse.Failure(exception.Error);
        }

        _charges.Add(quote);
        _audit.Record(access.User.Id, "ops.charges.quote_saved", nameof(OperationsQuote), quote.Id, after: new
        {
            number = quote.Number, customer = customer.Code, service = quote.ServiceCode, origin = from?.Code, destination = to.Code, standard = quote.StandardTotal, total = quote.Total,
            reason = quote.PreferredReason, validUntil = quote.ValidUntilUtc, priceVersion = rating.VersionId
        });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SaveOperationsQuoteResponse.Success(quote.Id, quote.Number);
        }

        // Both clicks saved at the same moment: the unique key kept the other one.
        _unitOfWork.DiscardChanges();
        return await _charges.FindQuoteByKeyAsync(key, cancellationToken) is { } raced
            ? SaveOperationsQuoteResponse.Success(raced.Id, raced.Number)
            : SaveOperationsQuoteResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
