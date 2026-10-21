using Microsoft.Extensions.Primitives;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsBranch;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsCountryContact;
using Vixly.Operations.Application.UseCases.Locations.SaveOperationsWarehouse;
using Vixly.Operations.Application.UseCases.Site.UpdateOperationsSiteDraft;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Web.Site;

// =========================================================
// FILE: OperationsEditorForms.cs
//
// PURPOSE:
// Turns the site editor forms into use-case requests and back (Slice 1B).
//
// RESPONSIBILITIES:
// - Read the site, branch, warehouse and contact forms
// - Fill the forms from saved records
// - Name the label of each site text
//
// IMPORTANT NOTES:
// - Unknown enum values fall back to the first value; the use cases validate everything else
// - Empty fields become null
// =========================================================
public static class OperationsEditorForms
{
    public static UpdateOperationsSiteDraftRequest Site(IFormCollection form, string language)
    {
        ArgumentNullException.ThrowIfNull(form);

        OperationsSiteServices services = OperationsSiteServices.None;
        foreach (string? code in form["services"])
        {
            services |= code is null ? OperationsSiteServices.None : Service(code);
        }

        return new UpdateOperationsSiteDraftRequest
        {
            DisplayName = Text(form, "displayName") ?? string.Empty,
            Preset = Enum<OperationsSitePreset>(form, "preset"),
            PrimaryColour = Text(form, "primaryColour") ?? string.Empty,
            AccentColour = Text(form, "accentColour") ?? string.Empty,
            Services = services,
            ShowSteps = Flag(form, "showSteps"),
            ShowFaq = Flag(form, "showFaq"),
            FacebookUrl = Text(form, "facebook"),
            InstagramUrl = Text(form, "instagram"),
            XUrl = Text(form, "x"),
            LinkedInUrl = Text(form, "linkedin"),
            YouTubeUrl = Text(form, "youtube"),
            TikTokUrl = Text(form, "tiktok"),
            Language = language,
            Texts = OperationsSiteTextKeys.All.Keys
                .Where(key => form.ContainsKey("text." + key))
                .ToDictionary(key => key, key => Text(form, "text." + key), StringComparer.Ordinal)
        };
    }

    public static SaveOperationsBranchRequest Branch(IFormCollection form)
    {
        ArgumentNullException.ThrowIfNull(form);

        return new SaveOperationsBranchRequest
        {
            BranchId = Id(form, "branchId"),
            Code = Text(form, "code") ?? string.Empty,
            Name = Text(form, "name") ?? string.Empty,
            Line1 = Text(form, "line1") ?? string.Empty,
            Line2 = Text(form, "line2"),
            City = Text(form, "city") ?? string.Empty,
            Region = Text(form, "region"),
            PostalCode = Text(form, "postalCode"),
            CountryCode = Text(form, "countryCode") ?? string.Empty,
            Phone = Text(form, "phone"),
            Email = Text(form, "email"),
            OpeningHours = Text(form, "openingHours"),
            ShowOnSite = Flag(form, "showOnSite"),
            Status = Enum<OperationsLocationStatus>(form, "status"),
            Side = form.ContainsKey("side") ? Enum<OperationsBranchSide>(form, "side") : OperationsBranchSide.Both
        };
    }

    public static SaveOperationsBranchRequest Branch(OperationsBranch branch)
    {
        ArgumentNullException.ThrowIfNull(branch);

        return new SaveOperationsBranchRequest
        {
            BranchId = branch.Id, Code = branch.Code, Name = branch.Name,
            Line1 = branch.Address.Line1, Line2 = branch.Address.Line2, City = branch.Address.City, Region = branch.Address.Region,
            PostalCode = branch.Address.PostalCode, CountryCode = branch.Address.CountryCode,
            Phone = branch.Phone, Email = branch.Email, OpeningHours = branch.OpeningHours,
            ShowOnSite = branch.ShowOnSite, Status = branch.Status, Side = branch.Side
        };
    }

    public static SaveOperationsWarehouseRequest Warehouse(IFormCollection form)
    {
        ArgumentNullException.ThrowIfNull(form);

        OperationsWarehouseRoles roles = OperationsWarehouseRoles.None;
        foreach (string? role in form["roles"])
        {
            roles |= System.Enum.TryParse(role, out OperationsWarehouseRoles value) && System.Enum.IsDefined(value) ? value : OperationsWarehouseRoles.None;
        }

        return new SaveOperationsWarehouseRequest
        {
            WarehouseId = Id(form, "warehouseId"),
            BranchId = Id(form, "branchId") ?? Guid.Empty,
            Code = Text(form, "code") ?? string.Empty,
            Name = Text(form, "name") ?? string.Empty,
            Line1 = Text(form, "line1") ?? string.Empty,
            Line2 = Text(form, "line2"),
            City = Text(form, "city") ?? string.Empty,
            Region = Text(form, "region"),
            PostalCode = Text(form, "postalCode"),
            CountryCode = Text(form, "countryCode") ?? string.Empty,
            Phone = Text(form, "phone"),
            Email = Text(form, "email"),
            OpeningHours = Text(form, "openingHours"),
            ShowOnSite = Flag(form, "showOnSite"),
            Status = Enum<OperationsLocationStatus>(form, "status"),
            Roles = roles,
            AcceptingDeliveries = Flag(form, "acceptingDeliveries"),
            TimeZone = Text(form, "timeZone"),
            ManagerUserId = Id(form, "manager"),
            DeliversHome = Flag(form, "deliversHome"),
            HomeArea = (Text(form, "homeArea") ?? string.Empty).Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Take(200).ToList(),
            HubWarehouseId = Id(form, "hub"),
            OperatedBy = Enum<OperationsDestinationOperator>(form, "operatedBy") is OperationsDestinationOperator.Partner ? OperationsDestinationOperator.Partner : OperationsDestinationOperator.Company,
            PartnerName = Text(form, "partnerName"),
            PartnerPhone = Text(form, "partnerPhone"),
            PartnerEmail = Text(form, "partnerEmail"),
        };
    }

    public static SaveOperationsWarehouseRequest Warehouse(OperationsWarehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(warehouse);

        return new SaveOperationsWarehouseRequest
        {
            WarehouseId = warehouse.Id, BranchId = warehouse.BranchId, Code = warehouse.Code, Name = warehouse.Name,
            Line1 = warehouse.Address.Line1, Line2 = warehouse.Address.Line2, City = warehouse.Address.City, Region = warehouse.Address.Region,
            PostalCode = warehouse.Address.PostalCode, CountryCode = warehouse.Address.CountryCode,
            Phone = warehouse.Phone, Email = warehouse.Email, OpeningHours = warehouse.OpeningHours,
            ShowOnSite = warehouse.ShowOnSite, Status = warehouse.Status, Roles = warehouse.Roles, AcceptingDeliveries = warehouse.AcceptingDeliveries,
            TimeZone = warehouse.TimeZone, ManagerUserId = warehouse.ManagerUserId,
            DeliversHome = warehouse.DeliversHome, HomeArea = warehouse.HomeArea, HubWarehouseId = warehouse.HubWarehouseId, OperatedBy = warehouse.OperatedBy,
            PartnerName = warehouse.PartnerName, PartnerPhone = warehouse.PartnerPhone, PartnerEmail = warehouse.PartnerEmail
        };
    }

    public static SaveOperationsCountryContactRequest Contact(IFormCollection form)
    {
        ArgumentNullException.ThrowIfNull(form);

        return new SaveOperationsCountryContactRequest
        {
            ContactId = Id(form, "contactId"),
            CountryCode = Text(form, "countryCode") ?? string.Empty,
            Label = Text(form, "label"),
            Phones = (Text(form, "phones") ?? string.Empty).Split(new[] { '\r', '\n', ',' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            WhatsApp = Text(form, "whatsApp"),
            Email = Text(form, "email"),
            Hours = Text(form, "hours"),
            SortOrder = int.TryParse(Text(form, "sortOrder"), out int order) ? Math.Clamp(order, 0, 999) : 0
        };
    }

    public static SaveOperationsCountryContactRequest Contact(OperationsCountryContact contact)
    {
        ArgumentNullException.ThrowIfNull(contact);

        return new SaveOperationsCountryContactRequest
        {
            ContactId = contact.Id, CountryCode = contact.CountryCode, Label = contact.Label, Phones = contact.Phones,
            WhatsApp = contact.WhatsApp, Email = contact.Email, Hours = contact.Hours, SortOrder = contact.SortOrder
        };
    }

    public static OperationsSiteServices Service(string code) => code switch
    {
        "air" => OperationsSiteServices.Air,
        "ocean" => OperationsSiteServices.Ocean,
        "consolidation" => OperationsSiteServices.Consolidation,
        "mailbox" => OperationsSiteServices.Mailbox,
        "door_to_door" => OperationsSiteServices.DoorToDoor,
        "pickup" => OperationsSiteServices.Pickup,
        _ => OperationsSiteServices.None
    };

    public static OperationsSiteImageKind? ImageKind(string code) => code switch
    {
        "logo" => OperationsSiteImageKind.Logo,
        "logodark" => OperationsSiteImageKind.LogoDark,
        "favicon" => OperationsSiteImageKind.Favicon,
        "hero" => OperationsSiteImageKind.Hero,
        _ => null
    };

    public static Guid? ImageId(OperationsBrandSettings settings, OperationsSiteImageKind kind)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return kind switch
        {
            OperationsSiteImageKind.Logo => settings.LogoImageId,
            OperationsSiteImageKind.LogoDark => settings.LogoDarkImageId,
            OperationsSiteImageKind.Favicon => settings.FaviconImageId,
            _ => settings.HeroImageId
        };
    }

    // "step.2.title" is labelled by "ui.editor.text.step.title" followed by its number.
    public static string LabelKey(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return string.Join('.', key.Split('.').Where(part => !part.All(char.IsAsciiDigit)));
    }

    public static string? Number(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return key.Split('.').FirstOrDefault(part => part.Length > 0 && part.All(char.IsAsciiDigit));
    }

    private static string? Text(IFormCollection form, string name)
    {
        StringValues values = form[name];
        string? value = values.Count > 0 ? values[0]?.Trim() : null;
        return string.IsNullOrEmpty(value) ? null : value;
    }

    private static bool Flag(IFormCollection form, string name) => string.Equals(Text(form, name), "true", StringComparison.OrdinalIgnoreCase);

    private static Guid? Id(IFormCollection form, string name) => Guid.TryParse(Text(form, name), out Guid id) && id != Guid.Empty ? id : null;

    private static TEnum Enum<TEnum>(IFormCollection form, string name)
        where TEnum : struct, System.Enum
    {
        return System.Enum.TryParse(Text(form, name), false, out TEnum value) && System.Enum.IsDefined(value) ? value : default;
    }
}
