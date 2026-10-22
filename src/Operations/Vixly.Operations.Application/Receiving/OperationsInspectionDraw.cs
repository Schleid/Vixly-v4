using System.Security.Cryptography;

namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsInspectionDraw.cs
//
// PURPOSE:
// Draws the random share of parcels sent to compliance inspection (Slice 3B, D-R4).
//
// RESPONSIBILITIES:
// - Say yes for about the company's share of parcels (5 in 100 unless it changed it)
//
// IMPORTANT NOTES:
// - A cryptographic random number, so nobody can predict which parcel is picked
// - The share is a company setting since 3C; tests replace the draw
// =========================================================
public class OperationsInspectionDraw
{
    public virtual bool Selects(int percent) => percent > 0 && RandomNumberGenerator.GetInt32(100) < percent;
}
