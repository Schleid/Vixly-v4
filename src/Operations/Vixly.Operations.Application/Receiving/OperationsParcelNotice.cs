using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsParcelNotice.cs
//
// PURPOSE:
// Payload of a parcel email (Slice 3B).
//
// RESPONSIBILITIES:
// - Who, which parcel, which email, and the tenant host for the link
//
// IMPORTANT NOTES:
// - Carries ids only; the handler reads the rest inside the tenant
// =========================================================
public sealed record OperationsParcelNotice(Guid CustomerId, Guid ParcelId, OperationsCustomerMailKind Kind, string PortalSlug);
