using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsBranch.cs
//
// PURPOSE:
// An office of the tenant in one country (Slice 1B, sweep D-O10).
//
// RESPONSIBILITIES:
// - Hold the code, name, address, contact, hours and side
// - Be shown on the public site or not
// - Deactivate and reactivate
//
// IMPORTANT NOTES:
// - Code unique per tenant (database)
// =========================================================
public sealed class OperationsBranch : OperationsEntity
{
    private OperationsBranch()
    {
        Code = string.Empty;
        Name = string.Empty;
        Address = null!;
    }

    private OperationsBranch(Guid tenantId, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        Code = string.Empty;
        Name = string.Empty;
        Address = null!;
    }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public OperationsPostalAddress Address { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? OpeningHours { get; private set; }

    public OperationsBranchSide Side { get; private set; }

    public OperationsLocationStatus Status { get; private set; }

    public bool ShowOnSite { get; private set; }

    public bool IsPublic => ShowOnSite && Status == OperationsLocationStatus.Active;

    public static OperationsBranch Create(Guid tenantId, OperationsLocationDetails details, OperationsBranchSide side, DateTimeOffset utcNow)
    {
        OperationsBranch branch = new(tenantId, utcNow);
        branch.Apply(details, side);
        return branch;
    }

    public void Update(OperationsLocationDetails details, OperationsBranchSide side, OperationsLocationStatus status, DateTimeOffset utcNow)
    {
        Apply(details, side);
        Status = Enum.IsDefined(status) ? status : OperationsLocationStatus.Active;
        MarkAsModified(utcNow);
    }

    private void Apply(OperationsLocationDetails details, OperationsBranchSide side)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(details.Address);

        Code = OperationsLocationText.Code(details.Code);
        Name = OperationsLocationText.Required(details.Name, OperationsLocationLimits.NameMaxLength, OperationsLocationErrors.NameRequired, OperationsLocationErrors.NameTooLong);
        Address = details.Address;
        Phone = OperationsLocationText.Phone(details.Phone);
        Email = OperationsLocationText.Email(details.Email);
        OpeningHours = OperationsLocationText.Optional(details.OpeningHours, OperationsLocationLimits.HoursMaxLength, OperationsLocationErrors.HoursTooLong);
        Side = Enum.IsDefined(side) ? side : OperationsBranchSide.Both;
        ShowOnSite = details.ShowOnSite;
    }
}
