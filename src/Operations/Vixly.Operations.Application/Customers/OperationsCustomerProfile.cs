using Vixly.Operations.Domain.Customers;

namespace Vixly.Operations.Application.Customers;

// =========================================================
// FILE: OperationsCustomerProfile.cs
//
// PURPOSE:
// A customer as the portal and the staff pages show them (Slice 2).
//
// RESPONSIBILITIES:
// - Carry the details, status, credit and mailbox addresses
//
// IMPORTANT NOTES:
// - Read-only
// =========================================================
public sealed record OperationsCustomerProfile(
    Guid CustomerId,
    string? Code,
    string FirstName,
    string LastName,
    string? CompanyName,
    string Email,
    string Phone,
    string Language,
    OperationsCustomerStatus Status,
    string? StatusReason,
    bool CreditAllowed,
    string? CreditReason,
    bool EmailVerified,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? LastSignInUtc,
    IReadOnlyList<OperationsMailboxAddress> Mailboxes)
{
    public string FullName => FirstName + " " + LastName;

    public static OperationsCustomerProfile Of(OperationsCustomer customer, IReadOnlyList<OperationsMailboxAddress> mailboxes)
    {
        ArgumentNullException.ThrowIfNull(customer);

        return new OperationsCustomerProfile(customer.Id, customer.Code, customer.FirstName, customer.LastName, customer.CompanyName, customer.Email.Value, customer.Phone,
            customer.PreferredLanguage.Value, customer.Status, customer.StatusReason, customer.CreditAllowed, customer.CreditReason, customer.EmailVerifiedUtc is not null,
            customer.CreatedUtc, customer.LastSignInUtc, mailboxes);
    }
}
