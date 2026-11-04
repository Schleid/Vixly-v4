using Android.App;
using Android.Content.PM;

namespace Vixly.PrintHelper;

// =========================================================
// FILE: MainActivity.cs
//
// PURPOSE:
// The Android activity of the print helper (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Start the MAUI app
//
// IMPORTANT NOTES:
// - Portrait only: the app has one simple page
// =========================================================
[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, ScreenOrientation = ScreenOrientation.Portrait,
    ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize)]
public sealed class MainActivity : MauiAppCompatActivity
{
}
