using Vixly.Operations.Web.Hosting;

namespace Vixly.Operations.Web.Security;

// =========================================================
// FILE: OperationsDeviceCookie.cs
//
// PURPOSE:
// Reads and writes the device cookie: the device id and its current secret (Slice 1C).
//
// RESPONSIBILITIES:
// - Write the cookie after an enrolment or a rotation
// - Read it back, or nothing when it is malformed
//
// IMPORTANT NOTES:
// - __Host-vx4-device: HttpOnly, Secure, host only; kept 400 days
// - Platform stores only a hash of the secret (Slice 7)
// =========================================================
public static class OperationsDeviceCookie
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromDays(400);

    public static void Write(HttpResponse response, Guid deviceId, string secret)
    {
        ArgumentNullException.ThrowIfNull(response);
        ArgumentException.ThrowIfNullOrWhiteSpace(secret);
        OperationsCookies.Write(response, OperationsCookies.Device, deviceId.ToString("N") + "." + secret, Lifetime);
    }

    public static (Guid DeviceId, string Secret)? Read(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        string? value = request.Cookies[OperationsCookies.Device];

        return value is { Length: > 33 } && value[32] == '.' && Guid.TryParseExact(value[..32], "N", out Guid deviceId)
            ? (deviceId, value[33..])
            : null;
    }

    public static void Delete(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);
        OperationsCookies.Delete(response, OperationsCookies.Device);
    }
}
