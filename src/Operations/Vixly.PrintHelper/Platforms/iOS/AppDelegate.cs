using Foundation;

namespace Vixly.PrintHelper;

// =========================================================
// FILE: AppDelegate.cs
//
// PURPOSE:
// The iOS application delegate of the print helper (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Create the MAUI app
//
// IMPORTANT NOTES:
// - Standard MAUI start-up
// =========================================================
[Register("AppDelegate")]
public sealed class AppDelegate : MauiUIApplicationDelegate
{
    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
