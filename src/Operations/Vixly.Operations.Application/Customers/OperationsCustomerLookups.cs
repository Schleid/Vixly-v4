using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Customers;

// =========================================================
// FILE: OperationsCustomerLookups.cs
//
// PURPOSE:
// Finds a customer the person is serving, never a list (owner, 2026-10-29: branches and roles design, 4.4, R-4).
//
// RESPONSIBILITIES:
// - Find by exact email, customer code or full phone number (at least its last 7 digits), at most a few matches
// - Record every lookup in the audit log, without what was typed
// - Refuse more than OperationsAccessLimits.CustomerLookupsPerHour lookups an hour to a person without the company's customer list
//
// IMPORTANT NOTES:
// - Used by the customer page, the front desk and the cashier
// - The caller decides who may look; this only finds, records and limits
// =========================================================
public sealed class OperationsCustomerLookups
{
    public const string ActionCode = "ops.lookups.customer";

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsAuditReader _audit;

    private readonly OperationsAuditTrail _trail;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OperationsCustomerLookups(IOperationsCustomerRepository customers, IOperationsAuditReader audit, OperationsAuditTrail trail, IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _trail = trail ?? throw new ArgumentNullException(nameof(trail));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<(IReadOnlyList<OperationsCustomer> Found, Error? Error)> FindAsync(OperationsAccessView access, string? text, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(access);

        string value = text?.Trim() ?? string.Empty;

        if (value.Length == 0)
        {
            return (Array.Empty<OperationsCustomer>(), null);
        }

        if (!access.HasTenantWide(OperationsPermissions.CustomersView))
        {
            (IReadOnlyList<Vixly.Operations.Domain.Audit.OperationsAuditEntry> _, int lastHour) = await _audit.ListAsync(
                new OperationsAuditFilter(access.User.Id, "lookups", _timeProvider.GetUtcNow().AddHours(-1), null), 1, 1, cancellationToken);

            if (lastHour >= OperationsAccessLimits.CustomerLookupsPerHour)
            {
                return (Array.Empty<OperationsCustomer>(), OperationsCustomerErrors.LookupLimit);
            }
        }

        IReadOnlyList<OperationsCustomer> found = await _customers.FindForCounterAsync(value, take, cancellationToken);
        _trail.Record(access.User.Id, ActionCode, nameof(OperationsCustomer), found.Count == 1 ? found[0].Id : null, after: new { found = found.Count });
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return (found, null);
    }
}
