using System.Globalization;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.UseCases.Customers.ChangeOperationsCustomerStatus;

// =========================================================
// FILE: ChangeOperationsCustomerStatusUseCase.cs
//
// PURPOSE:
// Staff suspend, reactivate or close a customer with a reason; the customer's sessions end (Slice 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ChangeOperationsCustomerStatusUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly IOperationsSettingsRepository _settings;

    public ChangeOperationsCustomerStatusUseCase(
        OperationsAccess access,
        IOperationsCustomerRepository customers,
        OperationsCustomerAccounts accounts,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        IOperationsSettingsRepository settings)
    {
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ChangeOperationsCustomerStatusResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsCustomerStatusRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.CustomersManage, false, cancellationToken);

        if (refused is not null)
        {
            return ChangeOperationsCustomerStatusResponse.Failure(refused);
        }

        if (await _customers.GetAsync(request.CustomerId, cancellationToken) is not { } customer)
        {
            return ChangeOperationsCustomerStatusResponse.Failure(OperationsCommonErrors.NotFound);
        }

        OperationsCustomerStatus before = customer.Status;

        // Owner, 2026-10-31: banning, and reopening a closed account, are for the Owner and administrators.
        if ((request.Status == OperationsCustomerStatus.Banned || before == OperationsCustomerStatus.Closed) && !(access!.IsOwner || access.IsAdministrator))
        {
            return ChangeOperationsCustomerStatusResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // The end day starts at midnight in the company's time zone.
        string? timeZone = (await _settings.FindAsync(cancellationToken))?.TimeZone;
        DateTimeOffset? until = request.SuspendedUntil is { } day ? OperationsTimeZones.At(day.ToDateTime(TimeOnly.MinValue), timeZone) : null;

        try
        {
            customer.ChangeStatus(request.Status, request.Reason, _accounts.UtcNow, until);
        }
        catch (DomainException exception)
        {
            return ChangeOperationsCustomerStatusResponse.Failure(exception.Error);
        }

        if (customer.Status != OperationsCustomerStatus.Active)
        {
            await _accounts.EndSessionsAsync(customer.Id, OperationsSessionEndReason.AccountRestricted, null, cancellationToken);
        }

        _audit.Record(access!.User.Id, "ops.customers.status_changed", nameof(OperationsCustomer), customer.Id,
            before: new { status = before.ToString() }, after: new { status = customer.Status.ToString(), until = request.SuspendedUntil?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) },
            reason: customer.StatusReason);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ChangeOperationsCustomerStatusResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
        }

        // Owner, 2026-10-31: the customer is told every time, in their language, without the internal reason.
        if (!string.IsNullOrWhiteSpace(request.PortalSlug) && customer.EmailVerifiedUtc is not null)
        {
            OperationsCustomerMailKind kind = customer.Status switch
            {
                OperationsCustomerStatus.Suspended => request.SuspendedUntil is null ? OperationsCustomerMailKind.AccountSuspended : OperationsCustomerMailKind.AccountSuspendedUntil,
                OperationsCustomerStatus.Closed => OperationsCustomerMailKind.AccountClosed,
                OperationsCustomerStatus.Banned => OperationsCustomerMailKind.AccountBanned,
                _ => OperationsCustomerMailKind.AccountReactivated
            };
            CultureInfo culture = CultureInfo.GetCultureInfo(customer.PreferredLanguage.Value);
            Dictionary<string, string> extra = new(StringComparer.Ordinal) { ["until"] = request.SuspendedUntil?.ToString("D", culture) ?? string.Empty };
            await _accounts.SendAsync(customer, kind, _accounts.SignInLink(request.PortalSlug), extra, new Dictionary<string, string>(StringComparer.Ordinal), cancellationToken);
        }

        return ChangeOperationsCustomerStatusResponse.Success();
    }
}
