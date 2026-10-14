namespace Vixly.Platform.Resources;

// =========================================================
// FILE: PlatformText.cs
//
// PURPOSE:
// Marker type for the Platform plane's text resources
// (PlatformText.resx, .fr.resx, .es.resx).
//
// RESPONSIBILITIES:
// - Name the resource set for IStringLocalizer<PlatformText>
//
// IMPORTANT NOTES:
// - Keys are error codes (platform.*), UI keys (ui.*) and
//   email keys (email.*). The key of an error text equals its
//   code (Global Standard 4.2)
// - fr and es values marked needs-review were drafted by
//   machine; a native reviewer clears them before release
//   (L-10)
// - No code beyond this marker (Global Standard 2)
// =========================================================
public sealed class PlatformText
{
    private PlatformText()
    {
    }
}
