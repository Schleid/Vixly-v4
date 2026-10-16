using System.Security.Cryptography;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformDemoLicensing.cs
//
// PURPOSE:
// Demo data for Slice 7: a few devices for each active demo tenant.
//
// RESPONSIBILITIES:
// - Approve up to two devices per tenant within its licences
// - Leave one device waiting for the tenant admin
// - Send one release to Vixly for review on the first tenant
//
// IMPORTANT NOTES:
// - Development only; off unless PlatformDemoData:SeedOnStartup is true
// - Safe to run again: a tenant that already has its seat row is skipped
// - Demo secrets are random and thrown away; these devices can never sign in
// =========================================================
public sealed class PlatformDemoLicensing
{
    private readonly IPlatformTenantQueries _tenants;

    private readonly IPlatformTenantRepository _tenantRecords;

    private readonly IPlatformSubscriptionRepository _subscriptions;

    private readonly IPlatformLicensingRepository _licensing;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _clock;

    public PlatformDemoLicensing(
        IPlatformTenantQueries tenants,
        IPlatformTenantRepository tenantRecords,
        IPlatformSubscriptionRepository subscriptions,
        IPlatformLicensingRepository licensing,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider clock)
    {
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _tenantRecords = tenantRecords ?? throw new ArgumentNullException(nameof(tenantRecords));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<string> SeedAsync(CancellationToken cancellationToken)
    {
        PagedResult<PlatformTenantListItem> tenants = await _tenants.ListAsync(
            new PlatformTenantListFilter(null, null, PlatformDemoTenants.Tag, null),
            PagedRequest.Create(1, PagedRequest.MaximumPageSize),
            cancellationToken);
        DateTimeOffset now = _clock.GetUtcNow();
        int devices = 0;
        bool reviewSent = false;

        foreach (PlatformTenantListItem item in tenants.Items.OrderBy(tenant => tenant.TenantCode, StringComparer.Ordinal))
        {
            if (await _tenantRecords.GetAsync(item.PlatformTenantId, cancellationToken) is not { Status: PlatformTenantStatus.Active } tenant
                || await _subscriptions.GetCurrentAsync(tenant.Id, cancellationToken) is not { Status: PlatformSubscriptionStatus.Active } subscription
                || await _licensing.GetSeatsAsync(tenant.Id, cancellationToken) is not null)
            {
                continue;
            }

            PlatformTenantLicensing seats = PlatformTenantLicensing.Create(tenant.Id, now);
            _licensing.AddSeats(seats);
            PlatformDeviceActor admin = PlatformDeviceActor.Tenant("demo-admin", "Demo admin");
            List<PlatformDeviceEvent> events = new();
            (string Name, PlatformDeviceKind Kind)[] wanted = { ("Front desk PC", PlatformDeviceKind.Pc), ("Warehouse scanner", PlatformDeviceKind.Scanner), ("Driver phone", PlatformDeviceKind.Phone) };
            PlatformDevice? first = null;

            for (int index = 0; index < wanted.Length; index++)
            {
                (PlatformDevice device, PlatformDeviceEvent requested) = PlatformDevice.Enrol(
                    tenant.Id, seats.NextNumber(now), wanted[index].Name, wanted[index].Kind, null, Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), admin, now.AddDays(-3 + index));
                _licensing.AddDevice(device);
                events.Add(requested);
                devices++;

                // The last device stays waiting for the tenant admin.
                if (index < wanted.Length - 1 && seats.SeatsInUse < subscription.Licences)
                {
                    PlatformDeviceChange approved = device.Approve(admin, now.AddDays(-3 + index).AddHours(1));
                    seats.Apply(approved, subscription.Licences, now);
                    events.Add(approved.Event);
                    first ??= device;
                }
            }

            if (!reviewSent && first is not null)
            {
                PlatformDeviceChange release = first.Release(PlatformDeviceReason.Broken, false, admin, now.AddHours(-2));
                seats.Apply(release, subscription.Licences, now);
                events.Add(release.Event);
                reviewSent = true;
            }

            _licensing.AddEvents(events);
        }

        return devices == 0
            ? "Demo licensing: devices already present."
            : await _unitOfWork.SaveChangesAsync(cancellationToken) ? $"Demo licensing: added {devices} device(s)." : "Demo licensing: devices could not be saved.";
    }
}
