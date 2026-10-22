using System.Collections.Concurrent;
using Vixly.Operations.Application.Devices;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: FakeDeviceClient.cs
//
// PURPOSE:
// Stands in for Platform's device calls in the HTTP tests (Slice 1C).
//
// RESPONSIBILITIES:
// - Keep devices, secrets and emailed links per tenant
// - Answer device checks like Platform
//
// IMPORTANT NOTES:
// - Tests only; AutoApprove makes a requested computer active at once
// - Activating a tenant on the first device is the test's job (FakePlatformClient)
// =========================================================
internal sealed class FakeDeviceClient : IOperationsDeviceClient
{
    private readonly ConcurrentDictionary<Guid, Device> _devices = new();

    private readonly ConcurrentDictionary<string, (Guid TenantId, bool FirstDevice)> _links = new();

    public bool AutoApprove { get; set; } = true;

    public List<(Guid TenantId, string Token)> FirstDeviceLinks { get; } = new();

    public List<OperationsPlatformActor> Actors { get; } = new();

    public Action<Guid>? OnFirstDevice { get; set; }

    public string? NextRotation { get; set; }

    public Task<OperationsPlatformCall<bool>> RequestFirstDeviceLinkAsync(OperationsPlatformActor actor, CancellationToken cancellationToken)
    {
        Actors.Add(actor);

        if (actor.Role != "owner")
        {
            return Task.FromResult(OperationsPlatformCall<bool>.Refused(OperationsDeviceErrors.NotAllowed));
        }

        string token = Guid.NewGuid().ToString("N");
        _links[token] = (actor.TenantId, true);
        FirstDeviceLinks.Add((actor.TenantId, token));
        return Task.FromResult(OperationsPlatformCall<bool>.Ok(true));
    }

    public Task<OperationsPlatformCall<OperationsDeviceEnrolment>> CompleteLinkAsync(Guid tenantId, string token, string name, string kind, OperationsDeviceOrigin origin,
        CancellationToken cancellationToken)
    {
        if (!_links.TryRemove(token, out (Guid TenantId, bool FirstDevice) link) || link.TenantId != tenantId)
        {
            return Task.FromResult(OperationsPlatformCall<OperationsDeviceEnrolment>.Refused(OperationsDeviceErrors.LinkInvalid));
        }

        OperationsDeviceEnrolment enrolment = Add(tenantId, name, kind, "active", "Owner", origin.UserAgent, origin.Country);

        if (link.FirstDevice)
        {
            OnFirstDevice?.Invoke(tenantId);
        }

        return Task.FromResult(OperationsPlatformCall<OperationsDeviceEnrolment>.Ok(enrolment));
    }

    public Task<OperationsPlatformCall<OperationsDeviceEnrolment>> EnrolAsync(OperationsPlatformActor actor, string name, string kind, CancellationToken cancellationToken)
    {
        Actors.Add(actor);
        return Task.FromResult(OperationsPlatformCall<OperationsDeviceEnrolment>.Ok(Add(actor.TenantId, name, kind, AutoApprove ? "active" : "pending", actor.Name, actor.UserAgent, actor.Country)));
    }

    public Dictionary<string, Guid> SecondFactorLinks { get; } = new(StringComparer.Ordinal);

    public Task<OperationsPlatformCall<bool>> CompleteSecondFactorResetAsync(Guid tenantId, string token, CancellationToken cancellationToken)
    {
        bool valid = SecondFactorLinks.TryGetValue(token, out Guid linkTenant) && linkTenant == tenantId && SecondFactorLinks.Remove(token);
        return Task.FromResult(valid ? OperationsPlatformCall<bool>.Ok(true) : OperationsPlatformCall<bool>.Refused(OperationsDeviceErrors.LinkInvalid));
    }

    public List<bool> Rotations { get; } = new();

    public Task<OperationsDeviceCheck> VerifyAsync(Guid tenantId, Guid deviceId, string secret, bool rotate, string? country, CancellationToken cancellationToken)
    {
        if (!_devices.TryGetValue(deviceId, out Device? device) || device.TenantId != tenantId || device.Secret != secret)
        {
            return Task.FromResult(new OperationsDeviceCheck(false, "device_unknown", "none", null));
        }

        if (device.Status != "active")
        {
            return Task.FromResult(new OperationsDeviceCheck(false, "device_" + device.Status, "none", null));
        }

        Rotations.Add(rotate);

        if (rotate && NextRotation is { } rotated)
        {
            NextRotation = null;
            device.Secret = rotated;
            return Task.FromResult(new OperationsDeviceCheck(true, null, "full", rotated, device.Name, device.Number, device.Country));
        }

        return Task.FromResult(new OperationsDeviceCheck(true, null, "full", null, device.Name, device.Number, device.Country));
    }

    public Task<OperationsPlatformCall<IReadOnlyList<OperationsDevice>>> ListAsync(OperationsPlatformActor actor, CancellationToken cancellationToken)
    {
        Actors.Add(actor);
        IReadOnlyList<OperationsDevice> list = _devices.Values.Where(device => device.TenantId == actor.TenantId)
            .Select(device => new OperationsDevice(device.Id, device.Number, device.Name, device.Kind, device.Status, device.Status == "active", device.RequestedBy, DateTimeOffset.UtcNow, null,
                Vixly.Shared.Kernel.Clients.VixlyUserAgents.Browser(device.UserAgent), device.Status == "active" ? DateTimeOffset.UtcNow : null,
                device.Status == "active" ? device.RequestedBy : null))
            .ToList();
        return Task.FromResult(OperationsPlatformCall<IReadOnlyList<OperationsDevice>>.Ok(list));
    }

    public int Seats { get; set; } = 5;

    public Task<OperationsPlatformCall<OperationsDeviceSeats>> SeatsAsync(OperationsPlatformActor actor, CancellationToken cancellationToken) =>
        Task.FromResult(OperationsPlatformCall<OperationsDeviceSeats>.Ok(new OperationsDeviceSeats(Seats,
            _devices.Values.Count(device => device.TenantId == actor.TenantId && device.Status is "active" or "release_pending"), null, null)));

    public Task<OperationsPlatformCall<bool>> ApproveAsync(OperationsPlatformActor actor, Guid deviceId, CancellationToken cancellationToken) => Set(actor, deviceId, "active");

    public Task<OperationsPlatformCall<bool>> DenyAsync(OperationsPlatformActor actor, Guid deviceId, CancellationToken cancellationToken) => Set(actor, deviceId, "denied");

    public Task<OperationsPlatformCall<bool>> ReleaseAsync(OperationsPlatformActor actor, Guid deviceId, string reason, CancellationToken cancellationToken) => Set(actor, deviceId, "released");

    public string StatusOf(Guid deviceId) => _devices[deviceId].Status;

    private Task<OperationsPlatformCall<bool>> Set(OperationsPlatformActor actor, Guid deviceId, string status)
    {
        Actors.Add(actor);

        if (!actor.StepUp)
        {
            return Task.FromResult(OperationsPlatformCall<bool>.Refused(OperationsDeviceErrors.NotAllowed));
        }

        if (!_devices.TryGetValue(deviceId, out Device? device) || device.TenantId != actor.TenantId)
        {
            return Task.FromResult(OperationsPlatformCall<bool>.Refused(OperationsDeviceErrors.Refused));
        }

        device.Status = status;
        return Task.FromResult(OperationsPlatformCall<bool>.Ok(true));
    }

    private OperationsDeviceEnrolment Add(Guid tenantId, string name, string kind, string status, string requestedBy, string? userAgent, string? country)
    {
        Device device = new(Guid.NewGuid(), tenantId, "D-" + (_devices.Count + 1).ToString("D6", System.Globalization.CultureInfo.InvariantCulture), name, kind, Guid.NewGuid().ToString("N"), status, requestedBy)
        {
            UserAgent = userAgent,
            Country = country
        };
        _devices[device.Id] = device;
        return new OperationsDeviceEnrolment(device.Id, device.Number, status, device.Secret);
    }

    private sealed class Device
    {
        public Device(Guid id, Guid tenantId, string number, string name, string kind, string secret, string status, string requestedBy)
        {
            Id = id;
            TenantId = tenantId;
            Number = number;
            Name = name;
            Kind = kind;
            Secret = secret;
            Status = status;
            RequestedBy = requestedBy;
        }

        public Guid Id { get; }

        public Guid TenantId { get; }

        public string Number { get; }

        public string Name { get; }

        public string Kind { get; }

        public string Secret { get; set; }

        public string Status { get; set; }

        public string RequestedBy { get; }

        public string? UserAgent { get; init; }

        public string? Country { get; init; }
    }
}
