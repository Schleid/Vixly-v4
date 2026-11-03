using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;

namespace Vixly.Operations.Infrastructure.Persistence;

// =========================================================
// FILE: OperationsSuspensionLift.cs
//
// PURPOSE:
// Lifts the customer suspensions whose end day has come (owner, 2026-10-31).
//
// RESPONSIBILITIES:
// - Find suspended customers with an end day in the past, in every company, and make them active again
//
// IMPORTANT NOTES:
// - System scope only (the housekeeping job): the company filter is off
// - The reason recorded is a fixed English line for the history; the customer was told the day by email when suspended
// =========================================================
internal sealed class OperationsSuspensionLift : IOperationsSuspensionLift
{
    public const string Reason = "Suspension ended on its end day.";

    private readonly OperationsDbContext _context;

    public OperationsSuspensionLift(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<int> RunAsync(int batchSize, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        List<OperationsCustomer> due = await _context.Customers
            .Where(customer => customer.Status == OperationsCustomerStatus.Suspended && customer.SuspendedUntil != null && customer.SuspendedUntil <= utcNow)
            .OrderBy(customer => customer.SuspendedUntil)
            .Take(Math.Clamp(batchSize, 1, 500))
            .ToListAsync(cancellationToken);
        int lifted = due.Count(customer => customer.LiftSuspension(Reason, utcNow));

        if (lifted > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return lifted;
    }
}
