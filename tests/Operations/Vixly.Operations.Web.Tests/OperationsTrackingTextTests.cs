using System.Xml.Linq;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsTrackingTextTests.cs
//
// PURPOSE:
// Proves every tracking code, hold reason and milestone has staff and customer wording in en, fr and es (Slice 3A).
//
// RESPONSIBILITIES:
// - Read the OperationsText resources and look for each key
//
// IMPORTANT NOTES:
// - Customer wording lives in CustomerText (the customer portal)
// =========================================================
public sealed class OperationsTrackingTextTests
{
    public static TheoryData<string> Languages => new() { "", ".fr", ".es" };

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryCode_Reason_AndMilestone_HasStaffWording(string language)
    {
        HashSet<string> keys = XDocument.Load(Path.Combine(Root(), "src", "Operations", "Vixly.Operations.Resources", "OperationsText" + language + ".resx"))
            .Root!.Elements("data").Select(item => (string)item.Attribute("name")!).ToHashSet(StringComparer.Ordinal);

        IEnumerable<string> expected = OperationsTrackingCodes.All.Select(code => "ui.tracking." + code.Name["ops.tracking.".Length..])
            .Concat(Enum.GetNames<OperationsHoldReasons>().Where(name => name != nameof(OperationsHoldReasons.None)).Select(name => "ui.hold." + name))
            .Concat(Enum.GetNames<OperationsMilestone>().Select(name => "ui.milestone." + name));

        List<string> missing = expected.Where(key => !keys.Contains(key)).ToList();
        Assert.True(missing.Count == 0, "Missing: " + string.Join(", ", missing));
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryCustomerVisibleCode_Reason_AndMilestone_HasCustomerWording(string language)
    {
        HashSet<string> keys = XDocument.Load(Path.Combine(Root(), "src", "Customer", "Vixly.Customer.Resources", "CustomerText" + language + ".resx"))
            .Root!.Elements("data").Select(item => (string)item.Attribute("name")!).ToHashSet(StringComparer.Ordinal);

        IEnumerable<string> expected = OperationsTrackingCodes.All.Where(code => code.Visibility == OperationsTrackingVisibility.Customer)
            .Select(code => "ui.tracking." + code.Name["ops.tracking.".Length..])
            .Concat(OperationsTrackingCodes.HoldReasonNames.Select(name => "ui.hold." + name))
            .Concat(Enum.GetNames<OperationsMilestone>().Select(name => "ui.milestone." + name));

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
