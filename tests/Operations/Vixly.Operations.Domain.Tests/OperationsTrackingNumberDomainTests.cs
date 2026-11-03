using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Tests;

// =========================================================
// FILE: OperationsTrackingNumberDomainTests.cs
//
// PURPOSE:
// Proves the shipment's tracking number rules (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Two letters and 11 digits, the last a check digit; shown as "VX 482 917 36 054"
// - A mistyped number is refused, never read as another one
// - The company's two letters; a shipment gets one number, once it is final
//
// IMPORTANT NOTES:
// - Pure domain, no database
// =========================================================
public sealed class OperationsTrackingNumberDomainTests
{
    private static readonly int[] Digits = { 4, 8, 2, 9, 1, 7, 3, 6, 0, 5 };

    [Fact]
    public void Number_HasTheLetters_TenDigits_AndACheckDigit_AndReadsBackHoweverTyped()
    {
        string number = OperationsTrackingNumbers.Make("VX", Digits);

        Assert.Equal(13, number.Length);
        Assert.StartsWith("VX4829173605", number, StringComparison.Ordinal);
        Assert.Equal(number, OperationsTrackingNumbers.Read(number));
        Assert.Equal(number, OperationsTrackingNumbers.Read(" vx " + number[2..5] + "-" + number[5..8] + " " + number[8..] + " "));

        string shown = OperationsTrackingNumbers.Format(number);
        Assert.Equal("VX 482 917 36 05" + number[^1], shown);
        Assert.Equal(number, OperationsTrackingNumbers.Read(shown));
    }

    [Fact]
    public void MistypedNumbers_AreRefused()
    {
        string number = OperationsTrackingNumbers.Make("VX", Digits);

        // Every single wrong digit fails the check.
        for (int index = 2; index < number.Length; index++)
        {
            for (char digit = '0'; digit <= '9'; digit++)
            {
                if (digit != number[index])
                {
                    string typed = number[..index] + digit + number[(index + 1)..];
                    Assert.Null(OperationsTrackingNumbers.Read(typed));
                }
            }
        }

        // Two neighbours swapped are caught too (Luhn misses only 0 and 9 side by side).
        string swapped = number[..2] + number[3] + number[2] + number[4..];
        Assert.Null(OperationsTrackingNumbers.Read(swapped));

        Assert.Null(OperationsTrackingNumbers.Read(null));
        Assert.Null(OperationsTrackingNumbers.Read("VX123"));
        Assert.Null(OperationsTrackingNumbers.Read("V1" + number[2..]));
        Assert.Null(OperationsTrackingNumbers.Read("SH26000002"));
        Assert.Throws<ArgumentException>(() => OperationsTrackingNumbers.Make("V", Digits));
        Assert.Throws<ArgumentException>(() => OperationsTrackingNumbers.Make("VX", new[] { 1, 2, 3 }));
    }

    [Fact]
    public void Company_ChoosesTwoLetters_VxByDefault()
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(Guid.NewGuid(), DateTimeOffset.UtcNow);
        Assert.Equal("VX", settings.TrackingPrefix);

        settings.ChangeTrackingPrefix(" gl ", DateTimeOffset.UtcNow);
        Assert.Equal("GL", settings.TrackingPrefix);

        foreach (string wrong in new[] { "G", "GLX", "G1", "", "É1" })
        {
            DomainException refused = Assert.Throws<DomainException>(() => settings.ChangeTrackingPrefix(wrong, DateTimeOffset.UtcNow));
            Assert.Equal("ops.settings.tracking_prefix_invalid", refused.Error.Code);
        }

        Assert.Equal("GL", settings.TrackingPrefix);
    }

    [Fact]
    public void Shipment_GetsOneNumber_OnlyOnceFinal()
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        OperationsShipment shipment = OperationsShipment.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), now);
        string first = OperationsTrackingNumbers.Make("VX", Digits);

        // Open: nothing yet.
        shipment.GiveTrackingNumber(first, now);
        Assert.Null(shipment.TrackingNumber);
        Assert.Throws<ArgumentException>(() => shipment.GiveTrackingNumber("VX123", now));
    }
}
