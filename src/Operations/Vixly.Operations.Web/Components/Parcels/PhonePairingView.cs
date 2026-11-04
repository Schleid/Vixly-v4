namespace Vixly.Operations.Web.Components.Parcels;

// =========================================================
// FILE: PhonePairingView.cs
//
// PURPOSE:
// The one-time code of a phone joining as Vixly Print, as the Printers page shows it (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Carry the code, its QR code and when it stops working
//
// IMPORTANT NOTES:
// - Shown once; never stored
// =========================================================
public sealed record PhonePairingView(string Code, string QrDataUri, DateTimeOffset ExpiresUtc);
