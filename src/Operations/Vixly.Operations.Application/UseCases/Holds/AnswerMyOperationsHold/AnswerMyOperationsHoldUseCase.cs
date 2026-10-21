using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Holds;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.AnswerMyOperationsHold;

// =========================================================
// FILE: AnswerMyOperationsHoldUseCase.cs
//
// PURPOSE:
// The customer answers a hold on their package: a document, the contents and value, or a choice for a damaged package (Slice 3E, Q-R7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A document is checked by its content and stored in the company's file store
// - Staff review it on the holds page; nothing is released by the customer alone
// =========================================================
public sealed class AnswerMyOperationsHoldUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsHoldRepository _holds;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsFileStore _files;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public AnswerMyOperationsHoldUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsTrackingRepository tracking,
        IOperationsHoldRepository holds,
        IOperationsSettingsRepository settings,
        IOperationsFileStore files,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _holds = holds ?? throw new ArgumentNullException(nameof(holds));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<AnswerMyOperationsHoldResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AnswerMyOperationsHoldRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return AnswerMyOperationsHoldResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        // Only the customer's own package; another's answers "not found" (S-3).
        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { } parcel || parcel.CustomerId != signedIn.Customer.Id)
        {
            return AnswerMyOperationsHoldResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        string? type = null;

        if (request.Document is { Length: > 0 } content)
        {
            type = content.Length <= OperationsHoldLimits.MaxDocumentBytes ? OperationsDocumentInspector.TypeOf(content) : null;

            if (type is null)
            {
                return AnswerMyOperationsHoldResponse.Failure(OperationsHoldErrors.DocumentInvalid);
            }
        }

        DateTimeOffset utcNow = _accounts.UtcNow;
        string currency = (await _settings.FindAsync(cancellationToken))?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency;
        string? stored = null;
        bool saved = false;

        try
        {
            if (type is not null)
            {
                stored = await _files.SaveAsync(context.RequiredTenantId, "answers", request.Document!, type, cancellationToken);
            }

            OperationsHoldDocument? document = stored is null
                ? null
                : new OperationsHoldDocument(stored, type!, request.FileName, request.Document!.Length, OperationsDocumentInspector.Sha256(request.Document));
            OperationsHoldAnswer answer;

            try
            {
                answer = OperationsHoldAnswer.Create(parcel, request.Reason, request.Contents, request.Value, currency, request.Choice, request.Note, document, utcNow);
            }
            catch (DomainException exception)
            {
                return AnswerMyOperationsHoldResponse.Failure(exception.Error);
            }

            // A newer answer to the same hold replaces the one still waiting for staff. One hold has one open answer, so the
            // replaced one is written first and the new one second, in one transaction: both or neither.
            if (await _holds.FindOpenAnswerAsync(parcel.Id, request.Reason, cancellationToken) is { } earlier)
            {
                earlier.Replace(utcNow);
                saved = await _unitOfWork.SaveInTwoStepsAsync(() => _holds.Add(answer), cancellationToken);
            }
            else
            {
                _holds.Add(answer);
                saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            if (saved)
            {
                return AnswerMyOperationsHoldResponse.Success(answer.Id);
            }

            // Sent twice at once (a double click): the other request's answer is the one waiting now.
            _unitOfWork.DiscardChanges();
            return await _holds.FindOpenAnswerAsync(parcel.Id, request.Reason, cancellationToken) is { } raced && raced.CustomerId == signedIn.Customer.Id
                ? AnswerMyOperationsHoldResponse.Success(raced.Id)
                : AnswerMyOperationsHoldResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }
        finally
        {
            if (!saved && stored is not null)
            {
                await _files.DeleteAsync(context.RequiredTenantId, stored, CancellationToken.None);
            }
        }
    }
}
