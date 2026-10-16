namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformLicensingCounts.cs
//
// PURPOSE:
// How a tenant's devices stand: holding seats, pending, awaiting review.
//
// RESPONSIBILITIES:
// - Carry counts for the Licensing section
//
// IMPORTANT NOTES:
// - From one grouped query
// =========================================================
public sealed record PlatformLicensingCounts(int HoldingSeats, int Pending, int AwaitingReview, int Idle);
