namespace Vixly.Operations.Domain.Settings;

// =========================================================
// FILE: OperationsDeliveryModes.cs
//
// PURPOSE:
// How a tenant hands parcels to customers (ADR-0018).
//
// RESPONSIBILITIES:
// - Pickup at a pickup point, home delivery, or both
//
// IMPORTANT NOTES:
// - Flags; stored as int
// =========================================================
[Flags]
public enum OperationsDeliveryModes
{
    None = 0,
    Pickup = 1,
    HomeDelivery = 2
}
