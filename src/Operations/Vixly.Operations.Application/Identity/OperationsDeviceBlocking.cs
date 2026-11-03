using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsDeviceBlocking.cs
//
// PURPOSE:
// Blocks a licensed device used from another country than its own, and tells the company's administrators (owner, R-6, 2026-10-21).
//
// RESPONSIBILITIES:
// - Say whether a device is blocked
// - Block it once: the block, its history entry and the administrators' notice in one commit
//
// IMPORTANT NOTES:
// - Owner's words: "block sign-in until company system admin unblock them" and "notify admin that a device is block"
// - The caller commits; a device already blocked is not blocked or announced again
// - No code, email or second factor lets the person through: only an administrator lifts it
// =========================================================
public sealed class OperationsDeviceBlocking
{
    private readonly IOperationsDeviceBlockRepository _blocks;

    private readonly OperationsOutboxWriter _outbox;

    private readonly OperationsAuditTrail _audit;

    private readonly TimeProvider _timeProvider;

    public OperationsDeviceBlocking(IOperationsDeviceBlockRepository blocks, OperationsOutboxWriter outbox, OperationsAuditTrail audit, TimeProvider timeProvider)
    {
        _blocks = blocks ?? throw new ArgumentNullException(nameof(blocks));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<bool> IsBlockedAsync(Guid deviceId, CancellationToken cancellationToken) =>
        deviceId != Guid.Empty && await _blocks.FindOpenAsync(deviceId, cancellationToken) is not null;

    public async Task BlockAsync(Guid tenantId, Guid deviceId, string? deviceName, string? deviceNumber, string registeredCountry, string country, Guid? userId,
        string? portalSlug, CancellationToken cancellationToken)
    {
        if (await _blocks.FindOpenAsync(deviceId, cancellationToken) is not null)
        {
            return;
        }

        OperationsDeviceBlock block = OperationsDeviceBlock.Block(tenantId, deviceId, deviceName, deviceNumber, registeredCountry, country, userId, _timeProvider.GetUtcNow());
        _blocks.Add(block);
        _audit.Record(userId, "ops.auth.device_blocked", "Device", deviceId,
            arguments: new { device = block.DeviceNumber, registered = block.RegisteredCountry, country = block.Country });
        _outbox.Write(OperationsDeviceBlockedNotice.MessageType, tenantId, block.Id, new OperationsDeviceBlockedNotice(block.Id, portalSlug));
    }
}
