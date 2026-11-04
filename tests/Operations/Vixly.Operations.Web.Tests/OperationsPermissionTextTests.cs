using System.Xml.Linq;
using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsPermissionTextTests.cs
//
// PURPOSE:
// Proves the role editor has words for every permission and every area, in en, fr and es.
//
// RESPONSIBILITIES:
// - Every area heading (ui.permissions.area.*) the role editor groups by
// - Every permission line (ui.permissions.*) a company can tick
//
// IMPORTANT NOTES:
// - Found on production 2026-10-25: 14 area headings showed their raw key
// =========================================================
public sealed class OperationsPermissionTextTests
{
    public static TheoryData<string> Languages => new() { "", ".fr", ".es" };

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryAreaAndPermission_HasWording(string language)
    {
        HashSet<string> keys = XDocument.Load(Path.Combine(Root(), "src", "Operations", "Vixly.Operations.Resources", "OperationsText" + language + ".resx"))
            .Root!.Elements("data").Select(item => (string)item.Attribute("name")!).ToHashSet(StringComparer.Ordinal);

        IEnumerable<string> editable = OperationsPermissions.All.Where(permission => !OperationsPermissions.SystemOnly.Contains(permission));
        IEnumerable<string> expected = editable.Select(permission => "ui.permissions.area." + permission.Split('.')[1]).Distinct()
            .Concat(editable.Select(permission => "ui.permissions." + permission));

        List<string> missing = expected.Where(key => !keys.Contains(key)).ToList();
        Assert.True(missing.Count == 0, "Missing: " + string.Join(", ", missing));
    }

    private static string Root()
    {
        string root = AppContext.BaseDirectory;

        while (!File.Exists(Path.Combine(root, "Vixly.Operations.slnx")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Repository root not found.");
        }

        return root;
    }
}
