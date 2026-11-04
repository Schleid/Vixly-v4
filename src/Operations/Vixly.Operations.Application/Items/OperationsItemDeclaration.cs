using System.Globalization;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Items;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.Items;

// =========================================================
// FILE: OperationsItemDeclaration.cs
//
// PURPOSE:
// Records the items a customer declares for a package (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Turn the customer's lines into items, matched to the company's catalogue for the description, customs code and unit
// - Sum them up as the package's contents and value
// - Replace a parcel's items when the customer corrects the declaration
//
// IMPORTANT NOTES:
// - The caller saves; nothing is saved here
// - The country of origin is never guessed: staff read it on the item
// - Blank lines are left out
// =========================================================
public sealed class OperationsItemDeclaration
{
    private readonly IOperationsItemRepository _items;

    public OperationsItemDeclaration(IOperationsItemRepository items)
    {
        _items = items ?? throw new ArgumentNullException(nameof(items));
    }

    // The lines with words; none: the old single declaration applies.
    public static IReadOnlyList<OperationsItemDraft> Filled(IReadOnlyList<OperationsItemDraft>? drafts) =>
        drafts?.Where(draft => !string.IsNullOrWhiteSpace(draft.Words)).ToList() ?? new List<OperationsItemDraft>();

    // "2 x Sneakers, 1 x Phone case" and the total value, for the package's contents and declared value.
    public static (string Contents, decimal Value) Summary(IReadOnlyList<OperationsItemDraft> drafts)
    {
        ArgumentNullException.ThrowIfNull(drafts);
        string contents = string.Join(", ", drafts.Select(draft => draft.Quantity.ToString("0.###", CultureInfo.InvariantCulture) + " x " + draft.Words!.Trim()));

        if (contents.Length > OperationsReceivingLimits.ContentsMaxLength)
        {
            contents = contents[..(OperationsReceivingLimits.ContentsMaxLength - 1)] + "\u2026";
        }

        return (contents, drafts.Sum(draft => draft.Quantity * draft.ValueEach));
    }

    // The items, checked and matched; refused as a whole when one line is wrong.
    public async Task<IReadOnlyList<OperationsParcelItem>> PrepareAsync(Guid tenantId, IReadOnlyList<OperationsItemDraft> drafts, string currencyCode, DateTimeOffset utcNow,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(drafts);

        if (drafts.Count > OperationsParcelItem.MaxPerParcel)
        {
            throw new DomainException(OperationsItemErrors.TooMany);
        }

        IReadOnlyList<OperationsItemCatalogueEntry> catalogue = drafts.Count == 0 ? Array.Empty<OperationsItemCatalogueEntry>() : await _items.ListCatalogueAsync(cancellationToken);
        List<OperationsParcelItem> items = new();

        foreach (OperationsItemDraft draft in drafts)
        {
            string? words = draft.Words is { Length: > OperationsParcelItem.WordsMaxLength } typed ? typed[..OperationsParcelItem.WordsMaxLength] : draft.Words;
            OperationsParcelItem item = OperationsParcelItem.Declare(tenantId, words, draft.Quantity, draft.ValueEach, currencyCode, draft.Used, items.Count + 1, utcNow);

            if (OperationsItemMatcher.Find(catalogue, draft.Words) is { } entry)
            {
                item.Suggest(entry, utcNow);
                entry.Used(utcNow);
            }

            items.Add(item);
        }

        return items;
    }

    public void AddForPreAlert(IReadOnlyList<OperationsParcelItem> items, OperationsPreAlert preAlert, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(preAlert);

        foreach (OperationsParcelItem item in items)
        {
            item.ForPreAlert(preAlert.Id, utcNow);
            _items.Add(item);
        }
    }

    public void AddForParcel(IReadOnlyList<OperationsParcelItem> items, OperationsParcel parcel, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(parcel);

        foreach (OperationsParcelItem item in items)
        {
            item.ForParcel(parcel.Id, utcNow);
            _items.Add(item);
        }
    }

    // The customer corrected the declaration (Slice 3E): one item from their words, to classify again.
    public async Task ReplaceAsync(OperationsParcel parcel, string contents, decimal value, string currencyCode, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parcel);

        foreach (OperationsParcelItem old in await _items.ListForParcelAsync(parcel.Id, parcel.PreAlertId, cancellationToken))
        {
            _items.Remove(old);
        }

        string words = contents.Length > OperationsParcelItem.WordsMaxLength ? contents[..OperationsParcelItem.WordsMaxLength] : contents;
        IReadOnlyList<OperationsParcelItem> items = await PrepareAsync(parcel.TenantId, new[] { new OperationsItemDraft(words, 1m, Math.Max(0m, value), false) }, currencyCode, utcNow,
            cancellationToken);
        AddForParcel(items, parcel, utcNow);
    }
}
