using System.Security.Cryptography;
using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.Tests.Fakes;

// =========================================================
// FILE: FakeLicensingStore.cs
//
// PURPOSE:
// In-memory seats, devices, challenges, links, history, read models, key checks and mailer for the Slice 7 tests.
//
// RESPONSIBILITIES:
// - Keep the licensing rows and answer the repository and query ports from them
// - Check device signatures with real ECDSA P-256, as the production adapter does
// - Record every email instead of sending it
//
// IMPORTANT NOTES:
// - Unique hashes are enforced like the database's unique indexes
// =========================================================
internal sealed class FakeLicensingStore : IPlatformLicensingRepository, IPlatformLicensingQueries, IPlatformDeviceKeys, IPlatformLicensingMailer
{
    public List<PlatformTenantLicensing> Seats { get; } = new();

    public List<PlatformDevice> Devices { get; } = new();

    public List<PlatformDeviceEvent> Events { get; } = new();

    public List<PlatformDeviceChallenge> Challenges { get; } = new();

    public List<PlatformDeviceLink> Links { get; } = new();

    public List<(string To, PlatformLicensingMail Mail)> Mails { get; } = new();

    public Task<PlatformTenantLicensing?> GetSeatsAsync(Guid platformTenantId, CancellationToken cancellationToken) => Task.FromResult(Seats.FirstOrDefault(item => item.PlatformTenantId == platformTenantId));

    public void AddSeats(PlatformTenantLicensing seats) => Seats.Add(seats);

    public Task<PlatformDevice?> GetDeviceAsync(Guid deviceId, CancellationToken cancellationToken) => Task.FromResult(Devices.FirstOrDefault(item => item.Id == deviceId));

    public void AddDevice(PlatformDevice device) => Devices.Add(device);

    public void AddEvents(IEnumerable<PlatformDeviceEvent> events) => Events.AddRange(events);

    public Task<int> CountPendingAsync(Guid platformTenantId, CancellationToken cancellationToken) => Task.FromResult(Devices.Count(item => item.PlatformTenantId == platformTenantId && item.Status == PlatformDeviceStatus.Pending));

    public Task<int> CountEndedByTenantSinceAsync(Guid platformTenantId, DateTimeOffset sinceUtc, CancellationToken cancellationToken)
    {
        return Task.FromResult(Devices.Count(item => item.PlatformTenantId == platformTenantId && item.EndedByTenant && item.EndedUtc >= sinceUtc));
    }

    public Task<int> CountHoldingSeatsAsync(Guid platformTenantId, CancellationToken cancellationToken) => Task.FromResult(Devices.Count(item => item.PlatformTenantId == platformTenantId && item.HoldsSeat));

    public Task<IReadOnlyList<PlatformDevice>> ListOpenDevicesAsync(Guid platformTenantId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformDevice>>(Devices.Where(item => item.PlatformTenantId == platformTenantId && (item.HoldsSeat || item.Status == PlatformDeviceStatus.Pending)).ToArray());
    }

    public Task<IReadOnlyList<Guid>> ListExpiredPendingAsync(DateTimeOffset createdBeforeUtc, int take, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<Guid>>(Devices.Where(item => item.Status == PlatformDeviceStatus.Pending && item.CreatedUtc <= createdBeforeUtc).Select(item => item.Id).Take(take).ToArray());
    }

    public void AddChallenge(PlatformDeviceChallenge challenge) => Challenges.Add(challenge);

    public Task<PlatformDeviceChallenge?> GetChallengeAsync(string challengeHash, CancellationToken cancellationToken) => Task.FromResult(Challenges.FirstOrDefault(item => item.ChallengeHash == challengeHash));

    public void AddLink(PlatformDeviceLink link) => Links.Add(link);

    public Task<PlatformDeviceLink?> GetLinkAsync(string tokenHash, CancellationToken cancellationToken) => Task.FromResult(Links.FirstOrDefault(item => item.TokenHash == tokenHash));

    public Task<IReadOnlyList<PlatformDeviceLink>> ListOpenLinksAsync(Guid platformTenantId, PlatformDeviceLinkKind kind, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformDeviceLink>>(Links.Where(item => item.PlatformTenantId == platformTenantId && item.Kind == kind && item.UsedUtc == null && item.VoidedUtc == null).ToArray());
    }

    public Task<int> CountLinkRequestsSinceAsync(Guid platformTenantId, DateTimeOffset sinceUtc, CancellationToken cancellationToken)
    {
        return Task.FromResult(Events.Count(item => item.PlatformTenantId == platformTenantId && item.EventCode == PlatformDeviceEventCodes.LinkSent && item.CreatedUtc >= sinceUtc));
    }

    public Task<PagedResult<PlatformDeviceItem>> ListDevicesAsync(PlatformDeviceFilter filter, PagedRequest page, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        PlatformDeviceItem[] items = Devices
            .Where(item => filter.PlatformTenantId is null || item.PlatformTenantId == filter.PlatformTenantId)
            .Where(item => filter.Status is null || item.Status == filter.Status)
            .Where(item => !filter.AwaitingReviewOnly || item.AwaitsReview)
            .Select(item => Item(item, utcNow))
            .ToArray();

        return Task.FromResult(new PagedResult<PlatformDeviceItem>(items.Skip(page.Skip).Take(page.PageSize).ToArray(), page, items.Length));
    }

    public Task<PlatformDeviceItem?> GetDeviceAsync(Guid deviceId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        return Task.FromResult(Devices.FirstOrDefault(item => item.Id == deviceId) is { } device ? Item(device, utcNow) : null);
    }

    public Task<IReadOnlyList<PlatformDeviceEventItem>> ListEventsAsync(Guid deviceId, CancellationToken cancellationToken)
    {
        return Task.FromResult<IReadOnlyList<PlatformDeviceEventItem>>(Events.Where(item => item.PlatformDeviceId == deviceId).OrderByDescending(item => item.CreatedUtc)
            .Select(item => new PlatformDeviceEventItem(item.CreatedUtc, item.EventCode, item.Reason, item.Note, item.Arguments, item.ActorName)).ToArray());
    }

    public Task<PlatformLicensingCounts> CountAsync(Guid platformTenantId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        PlatformDevice[] devices = Devices.Where(item => item.PlatformTenantId == platformTenantId).ToArray();
        return Task.FromResult(new PlatformLicensingCounts(devices.Count(item => item.HoldsSeat), devices.Count(item => item.Status == PlatformDeviceStatus.Pending), devices.Count(item => item.AwaitsReview), 0));
    }

    public bool IsValidPublicKey(string? publicKey)
    {
        using ECDsa? key = Import(publicKey);
        return key is not null;
    }

    public bool Verify(string publicKey, byte[] challenge, string? signature)
    {
        using ECDsa? key = Import(publicKey);

        try
        {
            return key is not null && signature is not null && key.VerifyData(challenge, Convert.FromBase64String(signature), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    public Task SendAsync(PlatformTenant tenant, PlatformTenantContact recipient, PlatformLicensingMail mail, CancellationToken cancellationToken)
    {
        Mails.Add((recipient.Email.Value, mail));
        return Task.CompletedTask;
    }

    private static ECDsa? Import(string? publicKey)
    {
        if (string.IsNullOrWhiteSpace(publicKey))
        {
            return null;
        }

        ECDsa key = ECDsa.Create();

        try
        {
            key.ImportSubjectPublicKeyInfo(Convert.FromBase64String(publicKey), out _);
            return key.KeySize == 256 ? key : null;
        }
        catch (Exception exception) when (exception is FormatException or CryptographicException)
        {
            key.Dispose();
            return null;
        }
    }

    private static PlatformDeviceItem Item(PlatformDevice device, DateTimeOffset utcNow)
    {
        return new PlatformDeviceItem(device.Id, device.PlatformTenantId, "Tenant", device.Number, device.Name, device.Kind, device.Proof, device.Status, device.SeatHeld, device.Reason,
            device.RequestedByName, device.CreatedUtc, device.ApprovedUtc, device.ReleaseRequestedUtc, device.EndedUtc, device.LastSeenUtc, false);
    }
}
