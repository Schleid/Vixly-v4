using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Persistence.Conversions;

// =========================================================
// FILE: PlatformValueConverters.cs
//
// PURPOSE:
// Maps Kernel value objects to plain columns.
//
// RESPONSIBILITIES:
// - Store EmailAddress, PhoneNumber and LanguageCode as text
// - Rebuild them through their validating factories on read
//
// IMPORTANT NOTES:
// - Reading through Create() means a bad value in the
//   database fails loudly instead of entering the domain
// =========================================================
internal static class PlatformValueConverters
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public static readonly ValueConverter<EmailAddress, string> Email =
        new(value => value.Value, value => EmailAddress.FromStored(value));

    public static readonly ValueConverter<PhoneNumber?, string?> Phone =
        new(
            value => value == null ? null : value.Value,
            value => value == null ? null : PhoneNumber.Create(value));

    public static readonly ValueConverter<PhoneNumber, string> RequiredPhone =
        new(value => value.Value, value => PhoneNumber.Create(value));

    public static readonly ValueConverter<IReadOnlyDictionary<string, string>, string> Arguments =
        new(
            value => JsonSerializer.Serialize(value, JsonOptions),
            value => JsonSerializer.Deserialize<Dictionary<string, string>>(value, JsonOptions) ?? new Dictionary<string, string>());

    public static readonly ValueComparer<IReadOnlyDictionary<string, string>> ArgumentsComparer =
        new(
            (left, right) => JsonSerializer.Serialize(left, JsonOptions) == JsonSerializer.Serialize(right, JsonOptions),
            value => JsonSerializer.Serialize(value, JsonOptions).GetHashCode(StringComparison.Ordinal),
            value => new Dictionary<string, string>(value, StringComparer.Ordinal));

    public static readonly ValueConverter<LanguageCode, string> Language =
        new(value => value.Value, value => LanguageCode.Create(value));
}
