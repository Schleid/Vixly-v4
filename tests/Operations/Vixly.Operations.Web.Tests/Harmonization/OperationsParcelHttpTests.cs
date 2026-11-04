using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Payables;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tickets;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Web.Hosting;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsParcelHttpTests.cs
//
// PURPOSE:
// Proves the Operations harmonization (owner, 2026-11-04, H-1 to H-5) on the demo day's data.
//
// RESPONSIBILITIES:
// - The counters in one command give exactly what one query per counter gave, over the whole company and over one warehouse
// - Each company counts only its own rows
// - The slim brand reads give the published name and settings, and an unsaved change in the same request
// - A warehouse or shipment read again in a request is the row already read
//
// IMPORTANT NOTES:
// - The old rules (one COUNT per counter) are kept below as the reference
// =========================================================
public sealed partial class OperationsParcelHttpTests
{
    [SqlServerFact]
    public async Task Counters_InOneCommand_MatchOneQueryPerCounter_AndNeverCountAnotherCompany()
    {
        Guid main = NewTenant("harmony-main", "Harmony Main");
        Guid second = NewTenant("harmony-two", "Harmony Two");
        IOptions<OperationsDemoDataOptions> options = Options.Create(new OperationsDemoDataOptions());

        foreach ((Guid tenant, string slug, bool first) in new[] { (main, "harmony-main", true), (second, "harmony-two", false) })
        {
            await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
            {
                scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
                await ActivatorUtilities.CreateInstance<OperationsDemoStaffSeeder>(scope.ServiceProvider, options).SeedAsync(tenant, slug, CancellationToken.None);
            }

            Assert.True(await ActivatorUtilities.CreateInstance<OperationsDemoScenario>(_host.Services, options).SeedAsync(tenant, slug, first, CancellationToken.None));
        }

        DateOnly today = DateOnly.FromDateTime(DateTime.UtcNow);
        HashSet<string> nonZero = new(StringComparer.Ordinal);

        foreach (Guid tenant in new[] { main, second })
        {
            await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            IOperationsDashboardReader reader = scope.ServiceProvider.GetRequiredService<IOperationsDashboardReader>();
            List<Guid> one = await context.Warehouses.OrderBy(item => item.Code).Select(item => item.Id).Take(1).ToListAsync();

            foreach (IReadOnlyCollection<Guid>? warehouses in new IReadOnlyCollection<Guid>?[] { null, one, new List<Guid>() })
            {
                Dictionary<string, IReadOnlyCollection<Guid>?> asks = Keys.ToDictionary(key => key, _ => warehouses, StringComparer.Ordinal);
                IReadOnlyDictionary<string, int> together = await reader.CountManyAsync(asks, today, CancellationToken.None);

                foreach (string key in Keys)
                {
                    int alone = await OneAsync(context, key, warehouses, today);
                    Assert.True(alone == together[key], $"{key} over {(warehouses is null ? "all" : warehouses.Count + " warehouses")}: {together[key]} together, {alone} alone");
                    if (alone > 0)
                    {
                        nonZero.Add(key);
                    }
                }
            }

            // Only what is asked comes back; an unknown key counts nothing.
            IReadOnlyDictionary<string, int> some = await reader.CountManyAsync(new Dictionary<string, IReadOnlyCollection<Guid>?> { ["holds"] = null, ["nothing"] = null }, today, CancellationToken.None);
            Assert.Equal(new[] { "holds", "nothing" }, some.Keys.Order(StringComparer.Ordinal));
            Assert.Equal(0, some["nothing"]);
        }

        // The demo day fills enough stages for the comparison to mean something (holds and open work in each company).
        Assert.True(nonZero.Count >= 3, "above zero: " + string.Join(", ", nonZero));

        // The second company's figures are its own: the main company's rows are never in them.
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(second);
            OperationsDbContext context = scope.ServiceProvider.GetRequiredService<OperationsDbContext>();
            int everywhere = await context.Parcels.IgnoreQueryFilters().CountAsync(item => item.HoldReasons != OperationsHoldReasons.None && (item.TenantId == main || item.TenantId == second));
            int own = (await scope.ServiceProvider.GetRequiredService<IOperationsDashboardReader>()
                .CountManyAsync(new Dictionary<string, IReadOnlyCollection<Guid>?> { ["holds"] = null }, today, CancellationToken.None))["holds"];
            Assert.Equal(await context.Parcels.IgnoreQueryFilters().CountAsync(item => item.HoldReasons != OperationsHoldReasons.None && item.TenantId == second), own);
            Assert.True(everywhere > own, "the main company has holds of its own");
        }
    }

    [SqlServerFact]
    public async Task SlimBrandReads_AndRowsReadAgain_GiveWhatTheFullReadsGave()
    {
        Guid tenant = NewTenant("harmony-brand", "Harmony Brand");
        using CookieClient owner = await OwnerAsync("harmony-brand");
        Guid warehouse = await WarehouseAsync(tenant);

        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(tenant);
            IOperationsBrandRepository brands = scope.ServiceProvider.GetRequiredService<IOperationsBrandRepository>();
            Assert.Equal("Harmony Brand", await brands.FindDisplayNameAsync(CancellationToken.None));
            Assert.Equal("Harmony Brand", (await brands.FindPublishedAsync(CancellationToken.None))!.DisplayName);

            // A brand loaded and changed in this request, not saved yet: the slim reads show the change, as the full read did.
            Vixly.Operations.Domain.Branding.OperationsTenantBrand brand = (await brands.FindAsync(CancellationToken.None))!;
            Assert.Equal(brand.DisplayName, await brands.FindDisplayNameAsync(CancellationToken.None));
            Assert.Same(brand.Published, await brands.FindPublishedAsync(CancellationToken.None));

            // A warehouse read again is the one this request already read, whether from the list or by itself.
            IOperationsLocationRepository locations = scope.ServiceProvider.GetRequiredService<IOperationsLocationRepository>();
            Vixly.Operations.Domain.Locations.OperationsWarehouse alone = (await locations.GetWarehouseAsync(warehouse, CancellationToken.None))!;
            Assert.Same(alone, await locations.GetWarehouseAsync(warehouse, CancellationToken.None));
            Assert.Same(alone, (await locations.ListWarehousesAsync(CancellationToken.None)).Single(item => item.Id == warehouse));
            Assert.Null(await locations.GetWarehouseAsync(Guid.NewGuid(), CancellationToken.None));
        }

        // Another company never gets this company's name or warehouse.
        Guid other = NewTenant("harmony-other", "Harmony Other");
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            scope.ServiceProvider.GetRequiredService<IOperationsTenantScope>().Enter(other);
            Assert.Null(await scope.ServiceProvider.GetRequiredService<IOperationsBrandRepository>().FindDisplayNameAsync(CancellationToken.None));
            Assert.Null(await scope.ServiceProvider.GetRequiredService<IOperationsLocationRepository>().GetWarehouseAsync(warehouse, CancellationToken.None));
        }

        // The pages still show the company's name.
        (HttpResponseMessage _, string home) = await owner.GetAsync("/staff");
        Assert.Contains("Harmony Brand", home, StringComparison.Ordinal);
    }

    private static readonly string[] Keys =
    {
        "to_inspect", "holds", "tickets", "billing_tickets", "service_tickets", "open_shipments", "to_dispatch", "arriving", "ready_for_pickup",
        "payments_to_verify", "invoices_overdue", "credit_charges", "bills_to_approve", "bills_overdue", "webhooks_off", "reviews_new"
    };

    // The rules as they were before H-4: one COUNT per counter (the reference).
    private static Task<int> OneAsync(OperationsDbContext context, string key, IReadOnlyCollection<Guid>? warehouses, DateOnly today)
    {
        List<Guid> mine = warehouses?.ToList() ?? new List<Guid>();
        bool all = warehouses is null;

        return key switch
        {
            "to_inspect" => context.Parcels.CountAsync(item => (all || (item.WarehouseId != null && mine.Contains(item.WarehouseId.Value)))
                && (item.Flags & (OperationsParcelFlags.InspectionPending | OperationsParcelFlags.InspectionInProgress)) != 0),
            "holds" => context.Parcels.CountAsync(item => (all || (item.WarehouseId != null && mine.Contains(item.WarehouseId.Value))) && item.HoldReasons != OperationsHoldReasons.None),
            "tickets" => context.Tickets.CountAsync(item => (all || mine.Contains(item.WarehouseId)) && item.Status == OperationsTicketStatus.Open && item.Topic == OperationsTicketTopic.Package),
            "billing_tickets" => context.Tickets.CountAsync(item => (all || mine.Contains(item.WarehouseId)) && item.Status == OperationsTicketStatus.Open && item.Topic == OperationsTicketTopic.Billing),
            "service_tickets" => context.Tickets.CountAsync(item => item.Status == OperationsTicketStatus.Open
                && (item.Topic == OperationsTicketTopic.PickupDelivery || item.Topic == OperationsTicketTopic.Claim || item.Topic == OperationsTicketTopic.Other)),
            "open_shipments" => context.Shipments.CountAsync(item => (all || mine.Contains(item.OriginWarehouseId)) && item.Status == OperationsShipmentStatus.Open && item.DestinationCountryCode != null),
            "to_dispatch" => context.Dispatches.CountAsync(item => (all || mine.Contains(item.WarehouseId))
                && (item.Status == OperationsDispatchStatus.Planned || item.Status == OperationsDispatchStatus.Closed)),
            "arriving" => context.Dispatches.CountAsync(item => item.Status == OperationsDispatchStatus.Departed && item.ReceivedUtc == null
                && (all || (item.DestinationWarehouseId != null && mine.Contains(item.DestinationWarehouseId.Value)))),
            "ready_for_pickup" => context.Parcels.CountAsync(item => (all || (item.WarehouseId != null && mine.Contains(item.WarehouseId.Value))) && item.Milestone == OperationsMilestone.ReadyForPickup),
            "payments_to_verify" => context.Payments.CountAsync(item => (all || mine.Contains(item.WarehouseId)) && item.Pending),
            "invoices_overdue" => context.Invoices.CountAsync(item => (all || mine.Contains(item.WarehouseId)) && item.Status == OperationsInvoiceStatus.Issued
                && (item.DueOn ?? item.IssuedOn) < today && item.Total - item.CreditedTotal - item.PaidTotal - item.PendingTotal + item.RefundedTotal > 0m),
            "credit_charges" => context.Invoices.CountAsync(item => item.Status == OperationsInvoiceStatus.Issued && item.OnAccount == OperationsAccountCharge.AwaitingApproval),
            "bills_to_approve" => context.Bills.CountAsync(item => item.Status == OperationsBillStatus.Entered),
            "bills_overdue" => context.Bills.CountAsync(item => (item.Status == OperationsBillStatus.Entered || item.Status == OperationsBillStatus.Approved) && item.DueOn < today && item.Amount - item.PaidTotal > 0m),
            "webhooks_off" => context.WebhookEndpoints.CountAsync(item => item.DisabledForFailures && !item.Active),
            "reviews_new" => context.SiteTestimonials.CountAsync(item => item.CustomerId != null && item.ReviewedUtc == null),
            _ => Task.FromResult(0)
        };
    }
}
