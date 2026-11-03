using System.Security.Cryptography;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Infrastructure.Persistence;

// =========================================================
// FILE: OperationsTrackingNumberSource.cs
//
// PURPOSE:
// The ten random digits of a new tracking number (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Draw them from the secure random source, so numbers cannot be guessed
//
// IMPORTANT NOTES:
// - The caller checks that the number is free (a unique index backs it)
// =========================================================
internal static class OperationsTrackingNumberSource
{
    public static string Next(string prefix)
    {
        int[] digits = new int[OperationsTrackingNumbers.RandomDigits];

        for (int index = 0; index < digits.Length; index++)
        {
            digits[index] = RandomNumberGenerator.GetInt32(10);
        }

        return OperationsTrackingNumbers.Make(prefix, digits);
    }
}
