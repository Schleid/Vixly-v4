using Vixly.Shared.Kernel.Localization;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsDeviceBlockedMail.cs
//
// PURPOSE:
// The email to one company administrator: a device is blocked and needs their attention (owner, R-6).
//
// RESPONSIBILITIES:
// - Carry what the administrator needs to decide: the device, who, where, when, and the link to its page
//
// IMPORTANT NOTES:
// - Written in the administrator's language with the company's name (never "Vixly")
// =========================================================
public sealed record OperationsDeviceBlockedMail(
    string ToAddress,
    string ToName,
    LanguageCode Language,
    string CompanyName,
    string DeviceName,
    string PersonName,
    string RegisteredCountry,
    string Country,
    DateTimeOffset BlockedUtc,
    string Link);
