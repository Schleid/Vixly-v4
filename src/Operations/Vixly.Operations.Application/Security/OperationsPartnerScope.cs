using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Application.Security;

// =========================================================
// FILE: OperationsPartnerScope.cs
//
// PURPOSE:
// What a partner agent may do, and where (Slice 7 step 4, R-8).
//
// RESPONSIBILITIES:
// - Carry the partner's warehouse, whether partner access is on, and whether money is handled there
// - Answer whether a permission is held at that warehouse
//
// IMPORTANT NOTES:
// - Off (Open false) when the plan does not include partner agents, Platform cannot answer, or the warehouse is no longer the partner's
// - Payments only where the company lets that partner handle money
// =========================================================
public sealed record OperationsPartnerScope(Guid WarehouseId, bool Open, bool HandlesMoney)
{
    public bool Holds(string permission) =>
        Open && (OperationsPermissions.PartnerAgent.Contains(permission, StringComparer.Ordinal)
            || (HandlesMoney && OperationsPermissions.PartnerMoney.Contains(permission, StringComparer.Ordinal)));
}
