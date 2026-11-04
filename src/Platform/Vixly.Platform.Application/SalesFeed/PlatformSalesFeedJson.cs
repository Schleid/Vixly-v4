using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedJson.cs
//
// PURPOSE:
// Writes a sale event and a batch exactly as contract version 1 spells them.
//
// RESPONSIBILITIES:
// - Write the envelope: id, type, occurredUtc, sequence, tenant, controlUrl, data
// - Join stored events into one batch body without changing a byte of them
//
// IMPORTANT NOTES:
// - Field order is fixed so the same event is always the same text
// - Never card or bank details: callers write only what the contract lists
// - totals.daily names no tenant: its tenant is null
// - Plain UTF-8 text (no HTML escaping, e.g. "+" in a phone stays "+"): the body is JSON for a server, never HTML
// =========================================================
public static class PlatformSalesFeedJson
{
    private static readonly JsonWriterOptions Options = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    public static string Event(
        Guid id,
        string type,
        DateTimeOffset occurredUtc,
        long sequence,
        PlatformSalesFeedTenant? tenant,
        string? controlUrl,
        Action<Utf8JsonWriter> data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(type);
        ArgumentNullException.ThrowIfNull(data);

        using MemoryStream stream = new();

        using (Utf8JsonWriter writer = new(stream, Options))
        {
            writer.WriteStartObject();
            writer.WriteString("id", id.ToString("D", CultureInfo.InvariantCulture));
            writer.WriteString("type", type);
            writer.WriteString("occurredUtc", PlatformSalesFeedMapping.Utc(occurredUtc));
            writer.WriteNumber("sequence", sequence);

            if (tenant is null)
            {
                writer.WriteNull("tenant");
            }
            else
            {
                writer.WriteStartObject("tenant");
                writer.WriteString("id", tenant.Id.ToString("D", CultureInfo.InvariantCulture));
                writer.WriteString("code", tenant.Code);
                writer.WriteString("legalName", tenant.LegalName);
                writer.WriteString("displayName", tenant.DisplayName);
                writer.WriteString("ownerEmail", tenant.OwnerEmail);
                writer.WriteString("billingEmail", tenant.BillingEmail);
                writer.WriteString("country", tenant.Country);

                if (tenant.Phone is not null)
                {
                    writer.WriteString("phone", tenant.Phone);
                }

                writer.WriteEndObject();
            }

            if (controlUrl is null)
            {
                writer.WriteNull("controlUrl");
            }
            else
            {
                writer.WriteString("controlUrl", controlUrl);
            }

            writer.WritePropertyName("data");
            writer.WriteStartObject();
            data(writer);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static string Batch(IEnumerable<string> payloads)
    {
        ArgumentNullException.ThrowIfNull(payloads);

        return "{\"events\":[" + string.Join(",", payloads) + "]}";
    }

    public static void Text(Utf8JsonWriter writer, string name, string? value)
    {
        ArgumentNullException.ThrowIfNull(writer);

        if (value is null)
        {
            writer.WriteNull(name);
        }
        else
        {
            writer.WriteString(name, value);
        }
    }
}
