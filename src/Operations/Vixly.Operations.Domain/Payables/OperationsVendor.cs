using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Payables;

// =========================================================
// FILE: OperationsVendor.cs
//
// PURPOSE:
// A company the tenant buys from that is neither a carrier nor a partner agent (Slice 8): a customs broker, the landlord, a box supplier.
//
// RESPONSIBILITIES:
// - Keep its name, usual category and contact
// - Be turned off rather than deleted once it has bills
//
// IMPORTANT NOTES:
// - Carriers and partner agents have their own records (Slices 6B and 7)
// =========================================================
public sealed class OperationsVendor : OperationsEntity
{
    public const int NameMaxLength = 120;

    public const int ContactMaxLength = 200;

    private OperationsVendor()
    {
        Name = string.Empty;
    }

    private OperationsVendor(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Name = string.Empty;
    }

    public string Name { get; private set; }

    public OperationsBillCategory Category { get; private set; }

    public string? Contact { get; private set; }

    public bool Active { get; private set; }

    public static OperationsVendor Create(Guid tenantId, string? name, OperationsBillCategory category, string? contact, DateTimeOffset utcNow)
    {
        OperationsVendor vendor = new(tenantId, utcNow) { Active = true };
        vendor.Change(name, category, contact, true, utcNow);
        return vendor;
    }

    public void Change(string? name, OperationsBillCategory category, string? contact, bool active, DateTimeOffset utcNow)
    {
        Name = OperationsPayableText.Required(name, NameMaxLength, OperationsPayableErrors.VendorInvalid);
        Category = Enum.IsDefined(category) ? category : throw new DomainException(OperationsPayableErrors.VendorInvalid);
        Contact = OperationsPayableText.Optional(contact, ContactMaxLength);
        Active = active;
        MarkAsModified(utcNow);
    }
}
