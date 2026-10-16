using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformInvoiceParty.cs
//
// PURPOSE:
// The customer as printed on an invoice, frozen when it is issued.
//
// RESPONSIBILITIES:
// - Hold the name, address lines, tax ID and email used on the document
//
// IMPORTANT NOTES:
// - Value object owned by the invoice
// - A later change to the tenant never changes an issued invoice
// =========================================================
public sealed class PlatformInvoiceParty
{
    private PlatformInvoiceParty()
    {
        Name = string.Empty;
        Address = string.Empty;
        Email = string.Empty;
    }

    public string Name { get; private set; }

    public string Address { get; private set; }

    public string? TaxId { get; private set; }

    public string Email { get; private set; }

    public static PlatformInvoiceParty Create(string? name, string? address, string? taxId, string? email)
    {
        string trimmedName = (name ?? string.Empty).Trim();
        string trimmedAddress = (address ?? string.Empty).Trim();
        string? trimmedTaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim();
        string trimmedEmail = (email ?? string.Empty).Trim();

        if (trimmedName.Length == 0 || trimmedName.Length > PlatformBillingLimits.NameMaxLength
            || trimmedAddress.Length > PlatformBillingLimits.AddressMaxLength
            || trimmedTaxId?.Length > PlatformBillingLimits.TaxIdMaxLength
            || trimmedEmail.Length > PlatformBillingLimits.EmailMaxLength)
        {
            throw new DomainException(PlatformBillingErrors.ProfileInvalid);
        }

        return new PlatformInvoiceParty { Name = trimmedName, Address = trimmedAddress, TaxId = trimmedTaxId, Email = trimmedEmail };
    }
}
