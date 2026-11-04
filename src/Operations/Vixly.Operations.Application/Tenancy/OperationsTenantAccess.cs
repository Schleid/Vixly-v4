namespace Vixly.Operations.Application.Tenancy;

// =========================================================
// FILE: OperationsTenantAccess.cs
//
// PURPOSE:
// How much a live tenant may do under its subscription (Platform's access level).
//
// RESPONSIBILITIES:
// - Name the access levels Platform reports
//
// IMPORTANT NOTES:
// - Read-only refuses commands; payment-required lets only owners and admins reach billing (Slice 1)
// =========================================================
public enum OperationsTenantAccess
{
    None = 0,
    Full = 1,
    ReadOnly = 2,
    PaymentRequired = 3
}
