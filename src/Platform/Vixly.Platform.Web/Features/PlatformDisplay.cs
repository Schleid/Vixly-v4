using System.Globalization;
using System.Text;
using Microsoft.Extensions.Localization;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Localization;

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
