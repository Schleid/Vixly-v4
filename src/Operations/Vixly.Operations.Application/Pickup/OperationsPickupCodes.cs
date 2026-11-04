using System.Security.Cryptography;
using System.Text;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.Pickup;

// =========================================================
// FILE: OperationsPickupCodes.cs
//
// PURPOSE:
// Issue and check a shipment's pickup code (Slice 7 step 2, R-5).
//
// RESPONSIBILITIES:
// - Give a new code, kept protected with the server's key; the one still valid, to send again
// - Check a typed code in constant time; a code older than its lifetime no longer works
//
// IMPORTANT NOTES:
// - The caller saves the shipment before the code is emailed
// =========================================================
public sealed class OperationsPickupCodes
{
    private readonly IOperationsSecretProtector _protector;

    private readonly TimeProvider _timeProvider;

    public OperationsPickupCodes(IOperationsSecretProtector protector, TimeProvider timeProvider)
    {
        _protector = protector ?? throw new ArgumentNullException(nameof(protector));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public string Issue(OperationsShipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        string code = OperationsPickupCode.New();
        shipment.SetPickupCode(_protector.Protect(shipment.Id.ToString("N") + ":" + code), _timeProvider.GetUtcNow());
        return code;
    }

    // The code still valid, or none.
    public string? Current(OperationsShipment shipment)
    {
        ArgumentNullException.ThrowIfNull(shipment);

        if (shipment.PickupCodeLocked || shipment.PickupCodeProtected is not { } stored || shipment.PickupCodeUtc is not { } issued || issued + OperationsPickupCode.Lifetime < _timeProvider.GetUtcNow()
            || _protector.Unprotect(stored) is not { } value || !value.StartsWith(shipment.Id.ToString("N") + ":", StringComparison.Ordinal))
        {
            return null;
        }

        return value[(value.IndexOf(':', StringComparison.Ordinal) + 1)..];
    }

    public bool Matches(OperationsShipment shipment, string? typed)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        string? expected = Current(shipment);
        string given = typed?.Trim() ?? string.Empty;
        return expected is not null && given.Length == OperationsPickupCode.Length
            && CryptographicOperations.FixedTimeEquals(Encoding.ASCII.GetBytes(expected), Encoding.ASCII.GetBytes(given));
    }
}
