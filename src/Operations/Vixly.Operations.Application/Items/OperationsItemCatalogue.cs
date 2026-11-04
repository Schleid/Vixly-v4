using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Items;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Items;

// =========================================================
// FILE: OperationsItemCatalogue.cs
//
// PURPOSE:
// The company's item catalogue, with the starter list loaded the first time (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - List the catalogue; the first time a member of staff needs it, copy the starter list into it
//
// IMPORTANT NOTES:
// - The caller saves
// - Loaded once per company, never again, even after the company removes every entry
// =========================================================
public sealed class OperationsItemCatalogue
{
    private readonly IOperationsItemRepository _items;

    private readonly IOperationsItemStarterList _starter;

    private readonly IOperationsSettingsRepository _settings;

    public OperationsItemCatalogue(IOperationsItemRepository items, IOperationsItemStarterList starter, IOperationsSettingsRepository settings)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
        _starter = starter ?? throw new ArgumentNullException(nameof(starter));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    // The catalogue; true when the starter list was just added (the caller then saves).
    public async Task<(IReadOnlyList<OperationsItemCatalogueEntry> Entries, bool Loaded)> ListAsync(Guid tenantId, bool mayLoad, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationsItemCatalogueEntry> entries = await _items.ListCatalogueAsync(cancellationToken);

        if (!mayLoad)
        {
            return (entries, false);
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        if (settings is { ItemCatalogueLoaded: true })
        {
            return (entries, false);
        }

        // A company without settings yet gets its defaults now, so the starter list is remembered as loaded.
        if (settings is null)
        {
            settings = OperationsTenantSettings.CreateDefault(tenantId, utcNow);
            _settings.Add(settings);
        }

        List<OperationsItemCatalogueEntry> all = entries.ToList();
        HashSet<string> known = new(entries.Select(entry => entry.Description), StringComparer.OrdinalIgnoreCase);

        foreach (OperationsStarterItem starter in _starter.Items.Where(item => known.Add(item.Description)))
        {
            OperationsItemCatalogueEntry entry = OperationsItemCatalogueEntry.Create(tenantId, starter.Description, starter.Matches, starter.HsCode, starter.Unit, utcNow);
            _items.AddCatalogueEntry(entry);
            all.Add(entry);
        }

        settings.ItemCatalogueWasLoaded(utcNow);
        return (all.OrderBy(entry => entry.Description, StringComparer.OrdinalIgnoreCase).ToList(), true);
    }
}
