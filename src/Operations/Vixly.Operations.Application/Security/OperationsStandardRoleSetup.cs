using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Access;

namespace Vixly.Operations.Application.Security;

// =========================================================
// FILE: OperationsStandardRoleSetup.cs
//
// PURPOSE:
// Gives the current company the standard roles it does not have yet (owner, 2026-10-29, R-5).
//
// RESPONSIBILITIES:
// - Create each missing standard role, named in the company's language
// - Never touch a role the company already has, renamed or not
//
// IMPORTANT NOTES:
// - Runs when the roles are first listed, as the Administrator role does; the caller saves
// - A name the company already uses for its own role gets " 2", so nothing is overwritten
// =========================================================
public sealed class OperationsStandardRoleSetup
{
    private readonly IOperationsRoleRepository _roles;

    private readonly IOperationsRoleNames _names;

    private readonly IOperationsPlatformClient _platform;

    private readonly TimeProvider _timeProvider;

    public OperationsStandardRoleSetup(IOperationsRoleRepository roles, IOperationsRoleNames names, IOperationsPlatformClient platform, TimeProvider timeProvider)
    {
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _names = names ?? throw new ArgumentNullException(nameof(names));
        _platform = platform ?? throw new ArgumentNullException(nameof(platform));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<int> EnsureAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationsRole> existing = await _roles.ListAsync(cancellationToken);
        HashSet<string> keys = existing.Where(role => role.TemplateKey is not null).Select(role => role.TemplateKey!).ToHashSet(StringComparer.Ordinal);
        List<(string Key, IReadOnlyList<string> Permissions)> missing = OperationsStandardRoles.All.Where(template => !keys.Contains(template.Key)).ToList();

        if (missing.Count == 0)
        {
            return 0;
        }

        string language = await LanguageAsync(tenantId, cancellationToken);
        HashSet<string> names = existing.Select(role => role.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        foreach ((string key, IReadOnlyList<string> permissions) in missing)
        {
            string name = _names.Name(key, language);

            for (int copy = 2; names.Contains(name); copy++)
            {
                name = _names.Name(key, language) + " " + copy.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            names.Add(name);
            _roles.Add(OperationsRole.Create(tenantId, name, permissions, utcNow, key));
        }

        return missing.Count;
    }

    // The company's default language from Platform; English when Platform cannot say.
    private async Task<string> LanguageAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        try
        {
            string? language = (await _platform.GetTenantAsync(tenantId, cancellationToken))?.DefaultLanguage?.Trim().ToLowerInvariant();
            return language is "fr" or "es" ? language : "en";
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return "en";
        }
    }
}
