using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence.Queries;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformLicensingDatabaseTests.cs
//
// PURPOSE:
// Proves the Slice 7 mapping against SQL Server: seats, devices, history, challenges, links and the read models.
//
// RESPONSIBILITIES:
// - Round-trip a device with its history, the seat row, a challenge and a link
// - One device number per tenant; one seat row per tenant; unique challenge and link hashes
// - Two hosts taking the last seat at once: the second save fails on the row version
//
// IMPORTANT NOTES:
// - Needs VIXLY_TEST_SQL_SERVER; skipped otherwise
// =========================================================
public sealed class PlatformLicensingDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 0, 0, 0, TimeSpan.Zero);

    private readonly PlatformDatabaseFixture _database;

    public PlatformLicensingDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task DeviceSeatsHistoryChallengeAndLink_RoundTrip_AndTheReadModelsFindThem()
    {
        PlatformTenant tenant = NewTenant();
        PlatformTenantLicensing seats = PlatformTenantLicensing.Create(tenant.Id, Now);
        PlatformDeviceActor admin = PlatformDeviceActor.Tenant("ops-1", "Grace Admin");
        (PlatformDevice device, PlatformDeviceEvent requested) = PlatformDevice.Enrol(tenant.Id, seats.NextNumber(Now), "Front desk PC", PlatformDeviceKind.Pc, null, new string('b', 64), admin, Now);
        PlatformDeviceChange approved = device.Approve(admin, Now.AddMinutes(1));
        seats.Apply(approved, 3, Now.AddMinutes(1));
        PlatformDeviceChallenge challenge = PlatformDeviceChallenge.Create(device.Id, Hash(), Now);
        string linkHash = Hash();
        PlatformDeviceLink link = PlatformDeviceLink.Create(PlatformDeviceLinkKind.OwnerRecovery, tenant.Id, null, linkHash, Now);

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.Add(tenant);
            PlatformLicensingRepository repository = new(context);
            repository.AddSeats(seats);
            repository.AddDevice(device);
            repository.AddEvents(new[] { requested, approved.Event });
            repository.AddChallenge(challenge);
            repository.AddLink(link);
            Assert.True(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
        }

        await using PlatformDbContext check = _database.NewContext();
        PlatformLicensingRepository store = new(check);

        PlatformTenantLicensing loadedSeats = (await store.GetSeatsAsync(tenant.Id, CancellationToken.None))!;
        Assert.Equal(1, loadedSeats.SeatsInUse);
        Assert.Equal("D-000002", loadedSeats.NextNumber(Now));
        PlatformDevice loaded = (await store.GetDeviceAsync(device.Id, CancellationToken.None))!;
        Assert.Equal(PlatformDeviceStatus.Active, loaded.Status);
        Assert.Equal(PlatformDeviceProof.Secret, loaded.Proof);
        Assert.Equal(PlatformDeviceSecretCheck.Valid, loaded.CheckSecret(new string('b', 64), Now.AddMinutes(2)));
        Assert.Equal(1, await store.CountHoldingSeatsAsync(tenant.Id, CancellationToken.None));
        Assert.Single(await store.ListOpenDevicesAsync(tenant.Id, CancellationToken.None));
        Assert.Equal(device.Id, (await store.GetChallengeAsync(challenge.ChallengeHash, CancellationToken.None))!.PlatformDeviceId);
        Assert.Equal(tenant.Id, (await store.GetLinkAsync(linkHash, CancellationToken.None))!.PlatformTenantId);
        Assert.Single(await store.ListOpenLinksAsync(tenant.Id, PlatformDeviceLinkKind.OwnerRecovery, CancellationToken.None));

        PlatformLicensingQueries queries = new(check);
        PagedResult<PlatformDeviceItem> listed = await queries.ListDevicesAsync(
            new PlatformDeviceFilter(tenant.Id, PlatformDeviceStatus.Active, null, false), PagedRequest.Create(1, 25), Now, CancellationToken.None);
        PlatformDeviceItem row = Assert.Single(listed.Items);
        Assert.Equal("D-000001", row.Number);
        Assert.Equal(tenant.Company.TradingName, row.TenantName);
        Assert.Equal(new[] { PlatformDeviceEventCodes.Approved, PlatformDeviceEventCodes.Requested },
            (await queries.ListEventsAsync(device.Id, CancellationToken.None)).Select(item => item.EventCode));
        PlatformLicensingCounts counts = await queries.CountAsync(tenant.Id, Now, CancellationToken.None);
        Assert.Equal(1, counts.HoldingSeats);
    }

    [SqlServerFact]
    public async Task DuplicateNumberAndSeatRow_FailToSave_AndTheLastSeatCannotBeTakenTwice()
    {
        PlatformTenant tenant = NewTenant();
        PlatformTenantLicensing seats = PlatformTenantLicensing.Create(tenant.Id, Now);
        PlatformDeviceActor admin = PlatformDeviceActor.Tenant("ops-1", "Grace Admin");

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.Add(tenant);
            context.TenantLicensing.Add(seats);
            context.Devices.Add(PlatformDevice.Enrol(tenant.Id, "D-000001", "First", PlatformDeviceKind.Pc, null, Hash(), admin, Now).Device);
            await context.SaveChangesAsync();
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Devices.Add(PlatformDevice.Enrol(tenant.Id, "D-000001", "Copy", PlatformDeviceKind.Pc, null, Hash(), admin, Now).Device);
            Assert.False(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.TenantLicensing.Add(PlatformTenantLicensing.Create(tenant.Id, Now));
            Assert.False(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
        }

        await using PlatformDbContext first = _database.NewContext();
        await using PlatformDbContext second = _database.NewContext();
        PlatformTenantLicensing a = (await new PlatformLicensingRepository(first).GetSeatsAsync(tenant.Id, CancellationToken.None))!;
        PlatformTenantLicensing b = (await new PlatformLicensingRepository(second).GetSeatsAsync(tenant.Id, CancellationToken.None))!;
        a.TakeSeat(1, Now);
        b.TakeSeat(1, Now);
        Assert.True(await new PlatformUnitOfWork(first).SaveChangesAsync(CancellationToken.None));
        Assert.False(await new PlatformUnitOfWork(second).SaveChangesAsync(CancellationToken.None));
    }

    [SqlServerFact]
    public async Task OwnerSupportRequest_RoundTrips_WithItsLink_AndOnlyOpenOnesAreListed()
    {
        PlatformTenant tenant = NewTenant();
        Guid alice = Guid.NewGuid();
        PlatformOwnerSupportRequest reset = PlatformOwnerSupportRequest.RequestSecondFactorReset(tenant.Id, "Lost phone and codes", "Called back the owner phone on file", alice, Now);
        PlatformOwnerSupportRequest done = PlatformOwnerSupportRequest.StartDeviceRecovery(tenant.Id, "Laptop stolen", "Called back", null, alice, Now.AddMinutes(-30));
        done.Complete(Now.AddMinutes(-10));
        string linkHash = Hash();

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.Tenants.Add(tenant);
            PlatformLicensingRepository repository = new(context);
            repository.AddSupportRequest(reset);
            repository.AddSupportRequest(done);
            Assert.True(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            PlatformLicensingRepository repository = new(context);
            PlatformOwnerSupportRequest loaded = (await repository.GetSupportRequestAsync(reset.Id, CancellationToken.None))!;
            loaded.Approve(Guid.NewGuid(), "Checked", Now.AddMinutes(1));
            repository.AddLink(PlatformDeviceLink.Create(PlatformDeviceLinkKind.SecondFactorReset, tenant.Id, null, loaded.Id, linkHash, Now.AddMinutes(1)));
            Assert.True(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
        }

        await using PlatformDbContext check = _database.NewContext();
        PlatformLicensingRepository store = new(check);
        PlatformOwnerSupportRequest open = Assert.Single(await store.ListOpenSupportRequestsAsync(tenant.Id, CancellationToken.None));
        Assert.Equal(reset.Id, open.Id);
        Assert.Equal(PlatformOwnerSupportStatus.Approved, open.Status);
        Assert.Equal(PlatformOwnerSupportKind.SecondFactorReset, open.Kind);
        Assert.Equal("Called back the owner phone on file", open.IdentityCheck);
        Assert.Equal("Checked", open.DecisionNote);
        PlatformDeviceLink link = (await store.GetLinkAsync(linkHash, CancellationToken.None))!;
        Assert.Equal(reset.Id, link.SupportRequestId);
        Assert.Equal(PlatformDeviceLinkKind.SecondFactorReset, link.Kind);
    }

    private static string Hash() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    private PlatformTenant NewTenant()
    {
        Guid actor = _database.ActorUserId == Guid.Empty ? Guid.NewGuid() : _database.ActorUserId;
        return PlatformTenant.CreateByStaff(
            "TEN-" + Guid.NewGuid().ToString("N")[..6].ToUpperInvariant(),
            "lic-" + Guid.NewGuid().ToString("N")[..8],
            PlatformCompanyDetails.Create("Orbit Freight LLC", "Orbit " + Guid.NewGuid().ToString("N")[..4], EmailAddress.Create("office@orbit.example"), PhoneNumber.Create("+1 305 555 0199"),
                PlatformPostalAddress.Create("1 Dock Road", null, "Miami", "FL", "33101", "US")),
            PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, "Ada Byron", EmailAddress.Create("owner@orbit.example"), null),
            LanguageCode.En,
            "America/New_York",
            actor,
            Now).Tenant;
    }
}
