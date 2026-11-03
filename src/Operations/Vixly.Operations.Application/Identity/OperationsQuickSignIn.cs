namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsQuickSignIn.cs
//
// PURPOSE:
// The limits of badge and PIN sign-in (Slice 12A, owner H-8).
//
// RESPONSIBILITIES:
// - Say how long a person stays on a device's PIN screen
// - Tell a simple PIN from an acceptable one
//
// IMPORTANT NOTES:
// - Four equal digits and runs up or down (1234, 9876) are refused
// =========================================================
public static class OperationsQuickSignIn
{
    public const int DevicePeopleMax = 30;

    public static readonly TimeSpan DevicePeopleWindow = TimeSpan.FromDays(30);

    public static bool IsAcceptablePin(string? pin)
    {
        if (!Vixly.Operations.Domain.Identity.OperationsStaffPin.IsWellFormed(pin))
        {
            return false;
        }

        int[] digits = pin!.Select(character => character - '0').ToArray();
        bool same = digits.All(digit => digit == digits[0]);
        bool up = Enumerable.Range(1, digits.Length - 1).All(index => digits[index] == digits[index - 1] + 1);
        bool down = Enumerable.Range(1, digits.Length - 1).All(index => digits[index] == digits[index - 1] - 1);
        return !same && !up && !down;
    }
}
