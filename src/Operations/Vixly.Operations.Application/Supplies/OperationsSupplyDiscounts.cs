using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Supplies;

// =========================================================
// FILE: OperationsSupplyDiscounts.cs
//
// PURPOSE:
// Decides whether a supplies discount may be given, and who approved it (Slice 5E, S-17 and S-18).
//
// RESPONSIBILITIES:
// - Apply the company's policy: clerks may discount or not, the largest line and order discounts without approval, a reason when required
// - Above the limit: approved by a person with the right who types their email and authenticator code on the clerk's screen (the clerk too when they hold it)
// - Say whether the order as it now stands keeps to the policy, after a quantity changes or a line goes (review of 5E)
//
// IMPORTANT NOTES:
// - ops.discounts.approve for the counter's warehouse; a clerk holding it approves with their own code too (a fresh second factor, never an open session alone)
// - A wrong code counts against the approver's account as a failed sign-in, and a locked account approves nothing (review of 5E)
// - Every refused approval says the same thing (it never tells whether the email exists)
// =========================================================
public sealed class OperationsSupplyDiscounts
{
    private readonly IOperationsUserRepository _users;

    private readonly OperationsAccess _access;

    private readonly OperationsStepUp _stepUp;

    public OperationsSupplyDiscounts(IOperationsUserRepository users, OperationsAccess access, OperationsStepUp stepUp)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _stepUp = stepUp ?? throw new ArgumentNullException(nameof(stepUp));
    }

    // Who approved the discount (null: within the clerk's limit), or the refusal.
    public async Task<(Guid? ApprovedBy, Error? Refused)> CheckAsync(OperationsSupplySale sale, Guid lineId, decimal percent, string? reason, OperationsAccessView clerk,
        OperationsWarehouse warehouse, OperationsTenantSettings settings, string? approverEmail, string? approverCode, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sale);
        ArgumentNullException.ThrowIfNull(clerk);
        ArgumentNullException.ThrowIfNull(warehouse);
        ArgumentNullException.ThrowIfNull(settings);

        if (percent == 0m)
        {
            return (null, null);
        }

        if (percent is < 0m or >= 100m || decimal.Round(percent, 2) != percent)
        {
            return (null, OperationsSupplySaleErrors.DiscountInvalid);
        }

        if (settings.SupplyDiscountReasonRequired && string.IsNullOrWhiteSpace(reason))
        {
            return (null, OperationsSupplySaleErrors.DiscountReasonRequired);
        }

        // The order's discount without the approved lines, with this line as asked.
        OperationsSupplySaleLine asked = sale.Line(lineId);
        decimal gross = sale.Gross;
        decimal others = sale.Lines.Where(line => line.Id != lineId && line.DiscountApprovedByUserId is null).Sum(line => line.Discount);
        decimal orderPercent = gross == 0m ? 0m : (others + decimal.Round(asked.Gross * percent / 100m, 2, MidpointRounding.AwayFromZero)) * 100m / gross;
        bool within = settings.SupplyDiscountsAllowed && percent <= settings.SupplyDiscountLinePercent && orderPercent <= settings.SupplyDiscountOrderPercent;

        if (within)
        {
            return (null, null);
        }

        if (string.IsNullOrWhiteSpace(approverEmail) && string.IsNullOrWhiteSpace(approverCode))
        {
            return (null, settings.SupplyDiscountsAllowed ? OperationsSupplySaleErrors.DiscountNeedsApproval : OperationsSupplySaleErrors.DiscountNotAllowed);
        }

        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        if (!EmailAddress.TryCreate(approverEmail, out EmailAddress? email) || await _users.FindByEmailAsync(email, cancellationToken) is not { } approver
            || !approver.Account.IsSignInAllowed(utcNow)
            // The approver's rights, read without a session of theirs (LoadAsync reads the user and their grants only).
            || await _access.LoadAsync(OperationsAuthorizationContext.ForTenant(sale.TenantId).WithUser(approver.Id, Guid.NewGuid()), cancellationToken) is not { } view
            || !view.CoversWarehouse(OperationsPermissions.DiscountsApprove, warehouse.Id, warehouse.BranchId))
        {
            return (null, OperationsSupplySaleErrors.ApproverInvalid);
        }

        return _stepUp.Check(view.User, approverCode) is null ? (approver.Id, null) : (null, OperationsSupplySaleErrors.ApproverInvalid);
    }

    // A wrong code for a known approver counts as a failed sign-in of theirs (the account locks as at sign-in). Saved by the caller.
    public async Task RecordFailureAsync(string? approverEmail, CancellationToken cancellationToken)
    {
        if (EmailAddress.TryCreate(approverEmail, out EmailAddress? email) && await _users.FindByEmailAsync(email, cancellationToken) is { } approver)
        {
            approver.RecordFailedSignIn(DateTimeOffset.UtcNow);
        }
    }

    // The order keeps to the policy as it now stands: its unapproved discounts within the line and order limits (a quantity changed, a line removed or added).
    public static bool Within(OperationsSupplySale sale, OperationsTenantSettings settings)
    {
        ArgumentNullException.ThrowIfNull(sale);
        ArgumentNullException.ThrowIfNull(settings);
        List<OperationsSupplySaleLine> unapproved = sale.Lines.Where(line => line.DiscountPercent > 0m && line.DiscountApprovedByUserId is null).ToList();

        if (unapproved.Count == 0)
        {
            return true;
        }

        decimal gross = sale.Gross;
        return settings.SupplyDiscountsAllowed && unapproved.All(line => line.DiscountPercent <= settings.SupplyDiscountLinePercent)
            && (gross == 0m || unapproved.Sum(line => line.Discount) * 100m / gross <= settings.SupplyDiscountOrderPercent);
    }
}
