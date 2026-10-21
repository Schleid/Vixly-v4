using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Infrastructure.Persistence;

// =========================================================
// FILE: OperationsValueConverters.cs
//
// PURPOSE:
// Database conversions of shared value objects.
//
// RESPONSIBILITIES:
// - Store email addresses and language codes as text
//
// IMPORTANT NOTES:
// - Values are validated again when read
// =========================================================
internal static class OperationsValueConverters
{
    public static readonly ValueConverter<EmailAddress, string> Email = new(value => value.Value, value => EmailAddress.FromStored(value));

    public static readonly ValueConverter<LanguageCode, string> Language = new(value => value.Value, value => LanguageCode.Create(value));
}
