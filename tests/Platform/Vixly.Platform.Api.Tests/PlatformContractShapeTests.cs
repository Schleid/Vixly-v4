using System.Reflection;
using System.Text;
using System.Text.Json;
using Vixly.Contracts.Platform.Common;

namespace Vixly.Platform.Api.Tests;

// =========================================================
// FILE: PlatformContractShapeTests.cs
//
// PURPOSE:
// Pins the wire shape of every Contracts.Platform internal record and event (Slice 9, D-K4).
//
// RESPONSIBILITIES:
// - Compare each record’s JSON names, types and nullability with the golden file
// - Prove the API writes camelCase names
//
// IMPORTANT NOTES:
// - Version 1 may only add fields: a removed or changed line fails here and needs a v2 and an ADR
// - Update the golden file in the same pull request as an additive change
// =========================================================
public sealed class PlatformContractShapeTests
{
    private static readonly NullabilityInfoContext Nullability = new();

    [Fact]
    public void InternalAndEventContracts_MatchTheGoldenShapes()
    {
        string golden = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "PlatformContractShapes.golden.txt")).Replace("\r\n", "\n", StringComparison.Ordinal).Trim();
        string actual = Shapes();

        if (Environment.GetEnvironmentVariable("VIXLY_WRITE_GOLDEN") is { Length: > 0 } path)
        {
            File.WriteAllText(path, actual + "\n");
        }

        Assert.Equal(golden, actual);
    }

    [Fact]
    public void Errors_AreWrittenInCamelCase()
    {
        string json = JsonSerializer.Serialize(new PlatformApiError("platform.common.not_found", "Not found.", new Dictionary<string, string> { ["max"] = "1" }, "abc"), new JsonSerializerOptions(JsonSerializerDefaults.Web));

        Assert.Equal("{\"code\":\"platform.common.not_found\",\"message\":\"Not found.\",\"arguments\":{\"max\":\"1\"},\"correlationId\":\"abc\"}", json);
    }

    private static string Shapes()
    {
        IEnumerable<Type> types = typeof(PlatformApiError).Assembly.GetTypes()
            .Where(type => type.IsPublic && type.Namespace is { } name
                && (name.StartsWith("Vixly.Contracts.Platform.Internal", StringComparison.Ordinal) || name == "Vixly.Contracts.Platform.Events" || name == "Vixly.Contracts.Platform.Common")
                && (!type.IsAbstract || type.IsSealed))
            .OrderBy(type => type.FullName, StringComparer.Ordinal);
        StringBuilder text = new();

        foreach (Type type in types)
        {
            IEnumerable<string> members = !(type.IsAbstract && type.IsSealed)
                ? type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(property => property.Name != "EqualityContract")
                    .Select(property => JsonNamingPolicy.CamelCase.ConvertName(property.Name) + ":" + Describe(property))
                : type.GetFields(BindingFlags.Public | BindingFlags.Static).Select(field => field.Name + "=" + field.GetValue(null));
            text.Append(type.FullName![("Vixly.Contracts.Platform.".Length)..]).Append(": ").AppendJoin(", ", members).Append('\n');
        }

        return text.ToString().Trim();
    }

    private static string Describe(PropertyInfo property)
    {
        NullabilityInfo info = Nullability.Create(property);
        return Name(property.PropertyType) + (info.ReadState == NullabilityState.Nullable && !property.PropertyType.IsValueType ? "?" : string.Empty);
    }

    private static string Name(Type type)
    {
        if (Nullable.GetUnderlyingType(type) is { } inner)
        {
            return Name(inner) + "?";
        }

        if (!type.IsGenericType)
        {
            return type.IsGenericParameter ? type.Name : type.Name switch
            {
                "String" => "string", "Int32" => "int", "Int64" => "long", "Boolean" => "bool", "Decimal" => "decimal",
                _ => type.Name
            };
        }

        string name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
        return name + "<" + string.Join(",", type.GetGenericArguments().Select(Name)) + ">";
    }
}
