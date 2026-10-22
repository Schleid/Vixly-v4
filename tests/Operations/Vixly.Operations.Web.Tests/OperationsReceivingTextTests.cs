using System.Reflection;
using System.Xml.Linq;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsReceivingTextTests.cs
//
// PURPOSE:
// Proves every receiving word exists in en, fr and es (Slice 3B).
//
// RESPONSIBILITIES:
// - Every receiving error, condition, photo kind, zone, check and new permission has staff wording
// - Every error a customer can meet on the portal has customer wording
//
// IMPORTANT NOTES:
// - Reads the resource files, so a key added in one language only fails here
// =========================================================
public sealed class OperationsReceivingTextTests
{
    public static TheoryData<string> Languages => new() { "", ".fr", ".es" };

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryReceivingWord_HasStaffWording(string language)
    {
        HashSet<string> keys = Keys("Operations", "Vixly.Operations.Resources", "OperationsText" + language);
        IEnumerable<string> expected = Errors()
            .Concat(OperationsTrackingCodes.Conditions.Select(value => "ui.receiving.condition." + value))
            .Concat(OperationsTrackingCodes.PhotoKinds.Select(value => "ui.receiving.photo." + value))
            .Concat(OperationsTrackingCodes.VerificationMethods.Select(value => "ui.parcels.verified." + value))
            .Concat(Enum.GetNames<OperationsStorageZone>().Select(name => "ui.storage.zone." + name.ToLowerInvariant()))
            .Concat(new[] { OperationsPermissions.ReceivingReceive, OperationsPermissions.ParcelsView, OperationsPermissions.ParcelsAssign }.Select(name => "ui.permissions." + name))
            .Concat(new[] { "parcel_received", "parcel_action_needed" }.SelectMany(kind => new[] { "email.customer." + kind + ".subject", "email.customer." + kind + ".body" }));

        List<string> missing = expected.Where(key => !keys.Contains(key)).ToList();
        Assert.True(missing.Count == 0, "Missing: " + string.Join(", ", missing));
    }

    [Theory]
    [MemberData(nameof(Languages))]
    public void EveryPortalError_HasCustomerWording(string language)
    {
        HashSet<string> keys = Keys("Customer", "Vixly.Customer.Resources", "CustomerText" + language);
        string[] portal =
        {
            "ops.receiving.tracking_number_invalid", "ops.receiving.carrier_invalid", "ops.receiving.text_too_long", "ops.receiving.value_invalid",
            "ops.receiving.pre_alert_not_open", "ops.receiving.pre_alert_duplicate", "ops.receiving.pre_alert_not_found", "ops.receiving.expected_date_invalid",
            "ops.receiving.parcel_not_found"
        };

        List<string> missing = portal.Concat(OperationsTrackingCodes.PhotoKinds.Select(value => "ui.packages.photo." + value)).Where(key => !keys.Contains(key)).ToList();
        Assert.True(missing.Count == 0, "Missing: " + string.Join(", ", missing));
    }

    // Every error the receiving rules can give (properties, and methods with arguments such as already_received).
    private static IEnumerable<string> Errors()
    {
        Type type = typeof(OperationsReceivingErrors);
        IEnumerable<Error> properties = type.GetProperties(BindingFlags.Public | BindingFlags.Static).Select(property => (Error)property.GetValue(null)!);
        IEnumerable<Error> methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static).Where(method => method.ReturnType == typeof(Error) && !method.IsSpecialName)
            .Select(method => (Error)method.Invoke(null, method.GetParameters().Select(_ => (object?)null).ToArray())!);
        return properties.Concat(methods).Select(error => error.Code).Distinct();
    }

    private static HashSet<string> Keys(string plane, string project, string file) =>
        XDocument.Load(Path.Combine(Root(), "src", plane, project, file + ".resx"))
            .Root!.Elements("data").Select(item => (string)item.Attribute("name")!).ToHashSet(StringComparer.Ordinal);

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
