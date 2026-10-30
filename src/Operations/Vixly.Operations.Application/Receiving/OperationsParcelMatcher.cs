using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsParcelMatcher.cs
//
// PURPOSE:
// Finds the customer of a scanned package (Slice 3B, ADR-0022 match order, D-R8).
//
// RESPONSIBILITIES:
// - Match the tracking numbers a scan stands for against the customers' open pre-alerts
//
// IMPORTANT NOTES:
// - Several customers may announce one number; then nobody gets the parcel until a clerk checks (review H1)
// - Never by name alone
// =========================================================
public sealed class OperationsParcelMatcher
{
    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsCustomerRepository _customers;

    public OperationsParcelMatcher(IOperationsReceivingRepository receiving, IOperationsCustomerRepository customers)
    {
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
    }

    public async Task<OperationsParcelMatch> MatchAsync(IReadOnlyCollection<string> trackingNumbers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(trackingNumbers);
        IReadOnlyList<OperationsPreAlert> open = await _receiving.ListOpenPreAlertsByTrackingAsync(trackingNumbers, cancellationToken);
        Dictionary<Guid, OperationsCustomer> owners = (await _customers.ListByIdsAsync(open.Select(preAlert => preAlert.CustomerId).Distinct().ToList(), cancellationToken))
            .Where(owner => owner.Status == OperationsCustomerStatus.Active)
            .ToDictionary(owner => owner.Id);
        List<OperationsPreAlert> live = open.Where(preAlert => owners.ContainsKey(preAlert.CustomerId)).ToList();

        // Several customers announced the same number with different spellings (USPS with or without the ZIP prefix): one customer is still one match.
        List<Guid> customers = live.Select(preAlert => preAlert.CustomerId).Distinct().ToList();

        return customers.Count switch
        {
            1 => new OperationsParcelMatch(live[0], owners[customers[0]], "pre_alert"),
            > 1 => new OperationsParcelMatch(null, null, "conflict"),
            _ => new OperationsParcelMatch(null, null, "unidentified")
        };
    }
}
