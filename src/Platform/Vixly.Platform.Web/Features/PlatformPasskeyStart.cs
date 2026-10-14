namespace Vixly.Platform.Web.Features;

// =========================================================
// FILE: PlatformPasskeyStart.cs
//
// PURPOSE:
// A passkey ceremony a presenter has started and the component
// now hands to the browser.
//
// RESPONSIBILITIES:
// - Carry the ceremony token (kept on the server) and the options
//   (sent to the browser)
//
// IMPORTANT NOTES:
// - The token never leaves the circuit; only the options go to
//   JavaScript
// =========================================================
public sealed record PlatformPasskeyStart(string CeremonyToken, string OptionsJson);
