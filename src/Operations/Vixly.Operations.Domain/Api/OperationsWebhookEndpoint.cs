using System.Net;
using System.Net.Sockets;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Api;

// =========================================================
// FILE: OperationsWebhookEndpoint.cs
//
// PURPOSE:
// An address of the company’s own system that receives its events, signed (Slice 10).
//
// RESPONSIBILITIES:
// - Check the address (HTTPS, a public host), keep the events chosen and the protected signing secret
// - Keep where it stands in each kind of event (the database version of the last event read); turn itself off after three days of failures
//
// IMPORTANT NOTES:
// - The signing secret is shown once; only its last characters after
// - Turned back on, it starts from now: what happened meanwhile is read through the API
// =========================================================
public sealed class OperationsWebhookEndpoint : OperationsEntity
{
    public const int UrlMaxLength = 500;

    public const int MaxPerCompany = 10;

    public static readonly TimeSpan FailingLimit = TimeSpan.FromDays(3);

    private OperationsWebhookEndpoint()
    {
        Url = string.Empty;
        SecretProtected = string.Empty;
        SecretHint = string.Empty;
    }

    private OperationsWebhookEndpoint(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Url = string.Empty;
        SecretProtected = string.Empty;
        SecretHint = string.Empty;
    }

    public string Url { get; private set; }

    public OperationsWebhookEvents Events { get; private set; }

    public string SecretProtected { get; private set; }

    public string SecretHint { get; private set; }

    public bool Active { get; private set; }

    // Turned off by itself after failing for three days (or by a person).
    public bool DisabledForFailures { get; private set; }

    public DateTimeOffset? LastSuccessUtc { get; private set; }

    public DateTimeOffset? FailingSinceUtc { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    // Where the address stands in each kind: the version of the last event queued (events are numbered by the database as they are saved).
    public long TrackingAfter { get; private set; }

    public long CustomersAfter { get; private set; }

    public long InvoicesAfter { get; private set; }

    public long PaymentsAfter { get; private set; }

    // A new address starts at the current version: what happened before it is read through the API.
    public static OperationsWebhookEndpoint Create(Guid tenantId, string? url, OperationsWebhookEvents events, string secretProtected, string secretHint, Guid by,
        long currentVersion, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(secretProtected);
        ArgumentException.ThrowIfNullOrWhiteSpace(secretHint);
        OperationsWebhookEndpoint endpoint = new(tenantId, utcNow)
        {
            SecretProtected = secretProtected,
            SecretHint = secretHint,
            CreatedByUserId = by,
            Active = true,
            TrackingAfter = currentVersion,
            CustomersAfter = currentVersion,
            InvoicesAfter = currentVersion,
            PaymentsAfter = currentVersion
        };
        endpoint.Change(url, events, currentVersion, utcNow);
        return endpoint;
    }

    public void Change(string? url, OperationsWebhookEvents events, long currentVersion, DateTimeOffset utcNow)
    {
        string checkedUrl = CheckUrl(url);

        if ((events & ~AllEvents) != 0 || events == OperationsWebhookEvents.None)
        {
            throw new DomainException(OperationsApiErrors.EventsRequired);
        }

        // A kind newly chosen starts from now.
        foreach (OperationsWebhookEvents kind in OperationsWebhookEventNames.All.Where(kind => events.HasFlag(kind) && !Events.HasFlag(kind)))
        {
            Advance(kind, currentVersion);
        }

        (Url, Events) = (checkedUrl, events);
        MarkAsModified(utcNow);
    }

    public long Cursor(OperationsWebhookEvents kind) => kind switch
    {
        OperationsWebhookEvents.Tracking => TrackingAfter,
        OperationsWebhookEvents.CustomerCreated => CustomersAfter,
        OperationsWebhookEvents.InvoiceIssued => InvoicesAfter,
        OperationsWebhookEvents.PaymentRecorded => PaymentsAfter,
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public void Advance(OperationsWebhookEvents kind, long afterVersion)
    {
        switch (kind)
        {
            case OperationsWebhookEvents.Tracking:
                TrackingAfter = afterVersion;
                break;
            case OperationsWebhookEvents.CustomerCreated:
                CustomersAfter = afterVersion;
                break;
            case OperationsWebhookEvents.InvoiceIssued:
                InvoicesAfter = afterVersion;
                break;
            case OperationsWebhookEvents.PaymentRecorded:
                PaymentsAfter = afterVersion;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(kind));
        }
    }

    public void Disable(DateTimeOffset utcNow)
    {
        Active = false;
        MarkAsModified(utcNow);
    }

    // Back on: from now, with a clean slate.
    public void Enable(long currentVersion, DateTimeOffset utcNow)
    {
        if (Active)
        {
            return;
        }

        (Active, DisabledForFailures, FailingSinceUtc) = (true, false, null);
        (TrackingAfter, CustomersAfter, InvoicesAfter, PaymentsAfter) = (currentVersion, currentVersion, currentVersion, currentVersion);
        MarkAsModified(utcNow);
    }

    public void Delivered(DateTimeOffset utcNow)
    {
        (LastSuccessUtc, FailingSinceUtc) = (utcNow, null);
        MarkAsModified(utcNow);
    }

    // A try failed: true when the address has failed for three days and is now turned off.
    public bool Failed(DateTimeOffset utcNow)
    {
        FailingSinceUtc ??= utcNow;

        if (Active && utcNow - FailingSinceUtc.Value >= FailingLimit)
        {
            (Active, DisabledForFailures) = (false, true);
        }

        MarkAsModified(utcNow);
        return DisabledForFailures && !Active;
    }

    public static OperationsWebhookEvents AllEvents { get; } =
        OperationsWebhookEvents.Tracking | OperationsWebhookEvents.CustomerCreated | OperationsWebhookEvents.InvoiceIssued | OperationsWebhookEvents.PaymentRecorded;

    // HTTPS (port 443 or 8443) to a public host: never a private, loopback or link-local address, never credentials in the address (SSRF).
    public static string CheckUrl(string? url)
    {
        string text = url?.Trim() ?? string.Empty;

        if (text.Length is 0 or > UrlMaxLength || !Uri.TryCreate(text, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps || uri.UserInfo.Length > 0
            || uri.Fragment.Length > 0 || uri.HostNameType is not (UriHostNameType.Dns or UriHostNameType.IPv4 or UriHostNameType.IPv6) || uri.Port is not (443 or 8443)
            || !HostIsPublic(uri))
        {
            throw new DomainException(OperationsApiErrors.UrlInvalid);
        }

        return uri.AbsoluteUri;
    }

    private static bool HostIsPublic(Uri uri)
    {
        string host = uri.IdnHost.TrimEnd('.').ToLowerInvariant();

        if (host is "localhost" || (!host.Contains('.', StringComparison.Ordinal) && uri.HostNameType == UriHostNameType.Dns)
            || host.EndsWith(".localhost", StringComparison.Ordinal) || host.EndsWith(".local", StringComparison.Ordinal) || host.EndsWith(".internal", StringComparison.Ordinal)
            || host.EndsWith(".lan", StringComparison.Ordinal) || host.EndsWith(".home.arpa", StringComparison.Ordinal))
        {
            return false;
        }

        return !IPAddress.TryParse(uri.HostNameType == UriHostNameType.IPv6 ? host.Trim('[', ']') : host, out IPAddress? address) || IsPublic(address);
    }

    // Also used when connecting: the address the name resolves to must be public too.
    public static bool IsPublic(IPAddress address)
    {
        ArgumentNullException.ThrowIfNull(address);

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (IPAddress.IsLoopback(address) || address.Equals(IPAddress.Any) || address.Equals(IPAddress.IPv6Any) || address.Equals(IPAddress.Broadcast))
        {
            return false;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            byte[] b = address.GetAddressBytes();
            return !(b[0] == 10 || b[0] == 0 || b[0] == 127 || (b[0] == 100 && b[1] >= 64 && b[1] <= 127) || (b[0] == 169 && b[1] == 254)
                || (b[0] == 172 && b[1] >= 16 && b[1] <= 31) || (b[0] == 192 && b[1] == 168) || (b[0] == 192 && b[1] == 0 && b[2] == 0) || (b[0] == 198 && (b[1] == 18 || b[1] == 19))
                || b[0] >= 224);
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            byte[] b = address.GetAddressBytes();
            // Also refused (review): forms that carry an IPv4 address inside (NAT64, IPv4-compatible, 6to4) and the discard range.
            return !(address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast || (b[0] & 0xFE) == 0xFC || address.IsIPv6Teredo
                || (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0d && b[3] == 0xb8)
                || (b[0] == 0x00 && b[1] == 0x64 && b[2] == 0xff && b[3] == 0x9b)
                || b.Take(12).All(part => part == 0)
                || (b[0] == 0x20 && b[1] == 0x02)
                || (b[0] == 0x01 && b[1] == 0x00 && b.Skip(2).Take(6).All(part => part == 0)));
        }

        return false;
    }
}
