using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Holds;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.SaveOperationsPaymentSettings;

// =========================================================
// FILE: SaveOperationsPaymentSettingsUseCase.cs
//
// PURPOSE:
// Change the payment methods the company accepts and its Zelle details (Slice 5F, C-1 and P-8).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.edit (company-wide)
// - Zelle is offered only with a tag or an email or phone, and its terms in English
// =========================================================
public sealed class SaveOperationsPaymentSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsFileStore _files;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsPaymentSettingsUseCase(
        OperationsAccess access,
        IOperationsSettingsRepository settings,
        IOperationsFileStore files,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsPaymentSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsPaymentSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsPaymentSettingsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        // Who customers pay (the Zelle details and picture) changes only after a fresh second factor, and both sides are kept (review of 5F).
        object before = Zelle(settings);
        bool payee = request.ZelleQr is { Length: > 0 } || request.RemoveZelleQr
            || !string.Equals(Trimmed(request.ZelleTag), settings.ZelleTag, StringComparison.Ordinal)
            || !string.Equals(Trimmed(request.ZelleContact), settings.ZelleContact, StringComparison.Ordinal)
            || !string.Equals(Trimmed(request.ZelleName), settings.ZelleName, StringComparison.Ordinal);

        if (payee && (await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, true, cancellationToken)).Refused is { } stepUp)
        {
            return SaveOperationsPaymentSettingsResponse.Failure(stepUp);
        }

        string? oldPicture = null;
        string? newPicture = null;
        string? pictureHash = null;

        // The QR picture: a PNG or JPEG of at most 1 MB, told by its first bytes, never its name.
        if (request.ZelleQr is { Length: > 0 } picture)
        {
            string? type = OperationsDocumentInspector.TypeOf(picture);

            if (picture.Length > 1024 * 1024 || type is not ("image/png" or "image/jpeg"))
            {
                return SaveOperationsPaymentSettingsResponse.Failure(OperationsPaymentErrors.PictureInvalid);
            }

            newPicture = await _files.SaveAsync(context.RequiredTenantId, "payments", picture, type, cancellationToken);
            pictureHash = OperationsDocumentInspector.Sha256(picture);
        }

        try
        {
            settings.ChangeZelle(request.ZelleTag, request.ZelleContact, request.ZelleName, request.ZelleTermsEn, request.ZelleTermsFr, request.ZelleTermsEs, utcNow);
            settings.ChangePayments(request.Cash, request.Zelle, request.Cheque, request.BankTransfer, request.Other1, request.Other2, request.Other3, utcNow);

            if (newPicture is not null || request.RemoveZelleQr)
            {
                oldPicture = settings.ChangeZelleQr(newPicture, utcNow);
            }
        }
        catch (DomainException exception)
        {
            if (newPicture is not null)
            {
                await _files.DeleteAsync(context.RequiredTenantId, newPicture, cancellationToken);
            }

            return SaveOperationsPaymentSettingsResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.payments.settings_saved", nameof(OperationsTenantSettings), settings.Id, before: before, after: new
        {
            cash = settings.AcceptCash, zelle = settings.AcceptZelle, cheque = settings.AcceptCheque, transfer = settings.AcceptBankTransfer,
            others = new[] { settings.OtherMethod1, settings.OtherMethod2, settings.OtherMethod3 }, payee = Zelle(settings), picture = pictureHash
        });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            if (newPicture is not null)
            {
                await _files.DeleteAsync(context.RequiredTenantId, newPicture, cancellationToken);
            }

            return SaveOperationsPaymentSettingsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        if (oldPicture is not null && oldPicture != newPicture)
        {
            await _files.DeleteAsync(context.RequiredTenantId, oldPicture, cancellationToken);
        }

        return SaveOperationsPaymentSettingsResponse.Success();
    }

    private static string? Trimmed(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static object Zelle(OperationsTenantSettings settings) => new
    {
        cash = settings.AcceptCash, zelle = settings.AcceptZelle, tag = settings.ZelleTag, contact = settings.ZelleContact, name = settings.ZelleName, picture = settings.ZelleQrKey
    };
}
