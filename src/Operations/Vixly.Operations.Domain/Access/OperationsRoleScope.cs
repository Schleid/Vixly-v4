namespace Vixly.Operations.Domain.Access;

// =========================================================
// FILE: OperationsRoleScope.cs
//
// PURPOSE:
// Where a role grant applies (Slice 1D).
//
// RESPONSIBILITIES:
// - Name the three scopes
//
// IMPORTANT NOTES:
// - A branch scope covers the branch and its warehouses
// =========================================================
public enum OperationsRoleScope
{
    Tenant = 0,
    Branch = 1,
    Warehouse = 2
}
