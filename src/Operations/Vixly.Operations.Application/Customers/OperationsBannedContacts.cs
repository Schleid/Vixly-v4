using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Customers;

// =========================================================
// FILE: OperationsBannedContacts.cs
//
// PURPOSE:
// Whether an email or a phone belongs to a customer the company banned (owner, 2026-10-31).
//
// RESPONSIBILITIES:
// - Compare an email with the company's customers, and a phone by its digits with the banned customers' phones
//
// IMPORTANT NOTES:
// - Only this company's customers (tenant filter); a ban never follows a person to another company
// - Phones compare by their digits, so "+509 3712-3456" and "+50937123456" are the same number
// =========================================================
public sealed class OperationsBannedContacts
{
    private readonly IOperationsCustomerRepository _customers;

    public OperationsBannedContacts(IOperationsCustomerRepository customers)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
    }

    public async Task<bool> IsBannedAsync(EmailAddress? email, string? phone, CancellationToken cancellationToken)
    {
        if (email is not null && await _customers.FindByEmailAsync(email, cancellationToken) is { Status: OperationsCustomerStatus.Banned })
        {
            return true;
        }

        string digits = Digits(phone);
        return digits.Length >= 7 && (await _customers.ListBannedPhonesAsync(cancellationToken)).Any(banned => Digits(banned) == digits);
    }

    private static string Digits(string? phone) => new((phone ?? string.Empty).Where(char.IsAsciiDigit).ToArray());
}
