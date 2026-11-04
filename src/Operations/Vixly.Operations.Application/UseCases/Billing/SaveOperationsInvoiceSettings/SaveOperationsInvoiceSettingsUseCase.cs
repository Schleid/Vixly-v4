using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.SaveOperationsInvoiceSettings;

// =========================================================
// FILE: SaveOperationsInvoiceSettingsUseCase.cs
//
// PURPOSE:
// Change the company's invoice settings (Slice 5F, F-5, I-4 and I-10).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.settings.edit (company-wide); the change and the settings before it are in the history
// =========================================================
public sealed class SaveOperationsInvoiceSettingsUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsInvoiceSettingsUseCase(
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

    public async Task<SaveOperationsInvoiceSettingsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsInvoiceSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.SettingsEdit, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsInvoiceSettingsResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
            _settings.Add(settings);
        }

        object before = Snapshot(settings);

        try
        {
            settings.ChangeInvoicing(request.TaxName, request.TaxNumber,
                new OperationsTaxRates(request.FreightTaxPercent, request.InsuranceTaxPercent, request.SuppliesTaxPercent, request.ChargesTaxPercent), request.TaxPerKind,
                request.NoteEn, request.NoteFr, request.NoteEs, utcNow);
        }
        catch (DomainException exception)
        {
            return SaveOperationsInvoiceSettingsResponse.Failure(exception.Error);
        }

        _audit.Record(access!.User.Id, "ops.invoices.settings_saved", nameof(OperationsTenantSettings), settings.Id, before: before, after: Snapshot(settings));

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveOperationsInvoiceSettingsResponse.Success()
            : SaveOperationsInvoiceSettingsResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    private static object Snapshot(OperationsTenantSettings settings) => new
    {
        name = settings.TaxName, number = settings.TaxNumber, freight = settings.FreightTaxPercent, insurance = settings.InsuranceTaxPercent, supplies = settings.SuppliesTaxPercent,
        charges = settings.ChargesTaxPercent, perKind = settings.TaxPerKind, noteEn = settings.InvoiceNoteEn, noteFr = settings.InvoiceNoteFr, noteEs = settings.InvoiceNoteEs
    };
}
