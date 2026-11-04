using System.Text.Json;
using Vixly.Operations.Application.Items;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Items;

namespace Vixly.Operations.Infrastructure.Items;

// =========================================================
// FILE: OperationsItemStarterList.cs
//
// PURPOSE:
// Reads the starter item catalogue shipped with the application (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Load item-starter-list.json once, keep it in memory
//
// IMPORTANT NOTES:
// - A line that cannot be read is skipped; a company never waits on it
// =========================================================
internal sealed class OperationsItemStarterList : IOperationsItemStarterList
{
    private const string ResourceName = "Vixly.Operations.Infrastructure.Items.item-starter-list.json";

    private readonly Lazy<IReadOnlyList<OperationsStarterItem>> _items = new(Load);

    public IReadOnlyList<OperationsStarterItem> Items => _items.Value;

    private static IReadOnlyList<OperationsStarterItem> Load()
    {
        using Stream? stream = typeof(OperationsItemStarterList).Assembly.GetManifestResourceStream(ResourceName);

        if (stream is null)
        {
            return Array.Empty<OperationsStarterItem>();
        }

        using JsonDocument document = JsonDocument.Parse(stream);
        List<OperationsStarterItem> items = new();

        foreach (JsonElement line in document.RootElement.GetProperty("items").EnumerateArray())
        {
            string? description = line.GetProperty("description").GetString();
            string? code = line.GetProperty("code").GetString();

            if (string.IsNullOrWhiteSpace(description) || code is not { Length: OperationsHsCode.MinDigits }
                || !Enum.TryParse(line.GetProperty("unit").GetString(), ignoreCase: true, out OperationsItemUnit unit))
            {
                continue;
            }

            items.Add(new OperationsStarterItem(description, line.GetProperty("matches").GetString() ?? string.Empty, code, unit));
        }

        return items;
    }
}
