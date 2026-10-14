using System.Globalization;
using System.Text;
using Microsoft.Extensions.Localization;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Localization;
using Vixly.Shared.UI.Primitives;

namespace Vixly.Platform.Web.Features;

// =========================================================
// FILE: PlatformDisplay.cs
//
// PURPOSE:
// Turns console values into resource keys and display text.
//
// RESPONSIBILITIES:
// - Map statuses, authority levels and admin actions to their
//   resource keys
// - Format UTC times in the reader's culture
//
// IMPORTANT NOTES:
// - Keys only: the text itself lives in PlatformText (UI-10)
// - Times show in UTC with the UTC label until the console
//   knows each user's time zone
// =========================================================
public static class PlatformDisplay
{
    private static readonly Lazy<IReadOnlyList<string>> TimeZones = new(() => TimeZoneInfo.GetSystemTimeZones()
        .Select(zone => zone.HasIanaId ? zone.Id : TimeZoneInfo.TryConvertWindowsIdToIanaId(zone.Id, out string? iana) ? iana : null)
        .OfType<string>()
        .Where(id => id.Contains('/', StringComparison.Ordinal))
        .Append("UTC")
        .Distinct(StringComparer.Ordinal)
        .Order(StringComparer.Ordinal)
        .ToList());

    public static string StatusKey(PlatformUserAccountStatus status)
    {
        return "ui.users.status." + Snake(status.ToString());
    }

    public static string AuthorityKey(PlatformUserAuthorityLevel level)
    {
        return "ui.users.authority." + Snake(level.ToString());
    }

    public static string ActionKey(PlatformUserAdminAction action)
    {
        return "ui.users.action." + Snake(action.ToString());
    }

    public static string RoleName(IStringLocalizer text, string name, string? systemKey)
    {
        ArgumentNullException.ThrowIfNull(text);

        return systemKey is null ? name : text["ui.roles.system." + systemKey].Value;
    }

    public static string TenantStatusKey(PlatformTenantStatus status)
    {
        return "ui.tenants.status." + Snake(status.ToString());
    }

    public static VxBadgeStatus TenantBadge(PlatformTenantStatus status)
    {
        return status switch
        {
            PlatformTenantStatus.Active => VxBadgeStatus.Active,
            PlatformTenantStatus.Suspended => VxBadgeStatus.Suspended,
            PlatformTenantStatus.Deactivated => VxBadgeStatus.Deactivated,
            PlatformTenantStatus.Banned => VxBadgeStatus.Locked,
            _ => VxBadgeStatus.Invited
        };
    }

    public static VxBadgeStatus RegistrationBadge(PlatformTenantRegistrationStatus status)
    {
        return status switch
        {
            PlatformTenantRegistrationStatus.Completed => VxBadgeStatus.Active,
            PlatformTenantRegistrationStatus.Cancelled => VxBadgeStatus.Locked,
            PlatformTenantRegistrationStatus.Expired => VxBadgeStatus.Deactivated,
            _ => VxBadgeStatus.Invited
        };
    }

    public static VxBadgeStatus UserBadge(PlatformUserAccountStatus status)
    {
        return status switch
        {
            PlatformUserAccountStatus.Active => VxBadgeStatus.Active,
            PlatformUserAccountStatus.Suspended => VxBadgeStatus.Suspended,
            PlatformUserAccountStatus.Locked => VxBadgeStatus.Locked,
            PlatformUserAccountStatus.Deactivated => VxBadgeStatus.Deactivated,
            _ => VxBadgeStatus.Invited
        };
    }

    public static VxBadgeStatus ActiveBadge(bool isActive)
    {
        return isActive ? VxBadgeStatus.Active : VxBadgeStatus.Suspended;
    }

    public static string CategoryKey(PlatformSuspensionCategory category)
    {
        return "ui.tenants.category." + Snake(category.ToString());
    }

    public static string ContactRoleKey(PlatformTenantContactRole role)
    {
        return "ui.tenants.contact_role." + Snake(role.ToString());
    }

    public static string RegistrationStatusKey(PlatformTenantRegistrationStatus status)
    {
        return "ui.registrations.status." + Snake(status.ToString());
    }

    public static string HistoryKey(string eventCode)
    {
        ArgumentNullException.ThrowIfNull(eventCode);

        return "ui." + eventCode["platform.".Length..];
    }

    public static string TenantAddress(string slug, string baseDomain)
    {
        return slug + "." + baseDomain;
    }

    public static string CountryName(IStringLocalizer text, string code)
    {
        ArgumentNullException.ThrowIfNull(text);

        return text["ui.country." + code].Value;
    }

    public static IReadOnlyList<(string Code, string Name)> Countries(IStringLocalizer text)
    {
        return PlatformCountryCodes.All
            .Select(code => (code, CountryName(text, code)))
            .OrderBy(country => country.Item2, StringComparer.Create(CultureInfo.CurrentUICulture, true))
            .ToList();
    }

    public static IReadOnlyList<string> TimeZoneIds => TimeZones.Value;

    public static string AuditActionKey(string actionCode)
    {
        return "ui.audit.action." + actionCode;
    }

    public static string PermissionKey(string permissionCode)
    {
        return "ui.permission." + permissionCode;
    }

    public static string PermissionAreaKey(string permissionCode)
    {
        ArgumentNullException.ThrowIfNull(permissionCode);

        return "ui.permission_area." + permissionCode.Split('.')[1];
    }

    public static string Utc(IStringLocalizer text, DateTimeOffset? value)
    {
        ArgumentNullException.ThrowIfNull(text);

        return value is null
            ? VixlyText.Format(text, "ui.common.never")
            : VixlyText.Format(text, "ui.common.utc_time", "time", value.Value.UtcDateTime.ToString("g", CultureInfo.CurrentCulture));
    }

    private static string Snake(string pascal)
    {
        StringBuilder builder = new(pascal.Length + 4);

        foreach (char character in pascal)
        {
            if (char.IsUpper(character) && builder.Length > 0)
            {
                builder.Append('_');
            }

            builder.Append(char.ToLowerInvariant(character));
        }

        return builder.ToString();
    }
}
