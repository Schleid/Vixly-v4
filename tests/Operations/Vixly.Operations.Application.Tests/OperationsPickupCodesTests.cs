using Vixly.Operations.Application.Pickup;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsPickupCodesTests.cs
//
// PURPOSE:
// Proves the pickup code is kept protected, bound to its shipment, and expires (Slice 7 step 2 review).
//
// RESPONSIBILITIES:
// - Issue, check in constant time, refuse another shipment's or an expired code
//
// IMPORTANT NOTES:
// - A reversible fake stands for Data Protection
// =========================================================
public sealed class OperationsPickupCodesTests
{
    [Fact]
    public void ACode_IsKeptProtected_BoundToItsShipment_AndExpires()
    {
        MovingClock clock = new(new DateTimeOffset(2026, 10, 21, 12, 0, 0, TimeSpan.Zero));
        OperationsPickupCodes codes = new(new FakeProtector(), clock);
        OperationsShipment shipment = OperationsShipment.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), clock.GetUtcNow());
        OperationsShipment other = OperationsShipment.Open(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), clock.GetUtcNow());

        string code = codes.Issue(shipment);
        Assert.DoesNotContain(code, shipment.PickupCodeProtected!, StringComparison.Ordinal);
        Assert.True(codes.Matches(shipment, " " + code));
        Assert.False(codes.Matches(shipment, code[..5]));
        Assert.Equal(code, codes.Current(shipment));

        // Copied onto another shipment, it does not work there.
        other.SetPickupCode(shipment.PickupCodeProtected!, clock.GetUtcNow());
        Assert.False(codes.Matches(other, code));

        clock.Now = clock.Now.AddDays(31);
        Assert.Null(codes.Current(shipment));
        Assert.False(codes.Matches(shipment, code));
    }

    private sealed class FakeProtector : IOperationsSecretProtector
    {
        public string Protect(string value) => "p:" + Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(value));

        public string? Unprotect(string protectedValue) =>
            protectedValue.StartsWith("p:", StringComparison.Ordinal) ? System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(protectedValue[2..])) : null;
    }

    private sealed class MovingClock : TimeProvider
    {
        public MovingClock(DateTimeOffset now) => Now = now;

        public DateTimeOffset Now { get; set; }

        public override DateTimeOffset GetUtcNow() => Now;
    }
}
