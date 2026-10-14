using System.Text;
using System.Text.RegularExpressions;

namespace Vixly.Shared.Kernel.Errors;

// =========================================================
// FILE: Error.cs
//
// PURPOSE:
// A language-neutral description of a failure: a stable code
// plus named, typed arguments.
//
// RESPONSIBILITIES:
// - Validate the code format (plane first, dot separated)
// - Validate argument names and restrict argument value types
//   to those that can be formatted in any culture
// - Provide value equality for tests
//
// IMPORTANT NOTES:
// - This is what Domain and Application return and throw
//   instead of text (Global Standard 5.5, ADR-0005). The
//   presentation layer maps the code to a localized message
//   and fills the arguments in the viewer's culture
// - Code examples: platform.identity.email_invalid,
//   shared.language.unsupported
// - Argument value types: string, int, long, decimal, bool,
//   DateOnly, DateTimeOffset
// - ToString() is English-free: the code and the arguments
//   only. It is meant for logs
// =========================================================
public sealed class Error : IEquatable<Error>
{
    private static readonly Regex CodePattern = new(
        @"^[a-z][a-z0-9_]*(\.[a-z][a-z0-9_]*)+$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Regex NamePattern = new(
        @"^[a-z][A-Za-z0-9]*$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private static readonly Type[] AllowedValueTypes =
    {
        typeof(string), typeof(int), typeof(long), typeof(decimal),
        typeof(bool), typeof(DateOnly), typeof(DateTimeOffset)
    };

    private Error(string code, IReadOnlyDictionary<string, object> arguments)
    {
        Code = code;
        Arguments = arguments;
    }

    public string Code { get; }

    public IReadOnlyDictionary<string, object> Arguments { get; }

    // =====================================================
    // METHOD: CREATE
    // =====================================================
    public static Error Create(string code, params (string Name, object Value)[] arguments)
    {
        ArgumentNullException.ThrowIfNull(code);
        ArgumentNullException.ThrowIfNull(arguments);

        if (!CodePattern.IsMatch(code))
        {
            throw new ArgumentException(
                $"Error code '{code}' must be lowercase, dot separated, with the plane first.",
                nameof(code));
        }

        SortedDictionary<string, object> values = new(StringComparer.Ordinal);

        foreach ((string name, object value) in arguments)
        {
            if (name is null || !NamePattern.IsMatch(name))
            {
                throw new ArgumentException(
                    $"Error argument name '{name}' must be camelCase.",
                    nameof(arguments));
            }

            if (value is null || !AllowedValueTypes.Contains(value.GetType()))
            {
                throw new ArgumentException(
                    $"Error argument '{name}' has an unsupported value type.",
                    nameof(arguments));
            }

            if (!values.TryAdd(name, value))
            {
                throw new ArgumentException(
                    $"Error argument '{name}' is declared twice.",
                    nameof(arguments));
            }
        }

        return new Error(code, values);
    }

    public bool Equals(Error? other)
    {
        if (other is null || Code != other.Code || Arguments.Count != other.Arguments.Count)
        {
            return false;
        }

        foreach ((string name, object value) in Arguments)
        {
            if (!other.Arguments.TryGetValue(name, out object? otherValue) || !value.Equals(otherValue))
            {
                return false;
            }
        }

        return true;
    }

    public override bool Equals(object? obj)
    {
        return Equals(obj as Error);
    }

    public override int GetHashCode()
    {
        HashCode hash = new();
        hash.Add(Code, StringComparer.Ordinal);

        foreach ((string name, object value) in Arguments)
        {
            hash.Add(name, StringComparer.Ordinal);
            hash.Add(value);
        }

        return hash.ToHashCode();
    }

    public override string ToString()
    {
        if (Arguments.Count == 0)
        {
            return Code;
        }

        StringBuilder builder = new(Code);
        builder.Append('(');
        builder.AppendJoin(", ", Arguments.Select(argument =>
            string.Create(
                System.Globalization.CultureInfo.InvariantCulture,
                $"{argument.Key}={argument.Value}")));
        builder.Append(')');

        return builder.ToString();
    }
}
