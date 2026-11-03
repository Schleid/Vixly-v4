using Android.App;
using Android.Runtime;

namespace Vixly.PrintHelper;

// =========================================================
// FILE: MainApplication.cs
//
// PURPOSE:
// The Android application of the print helper (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Create the MAUI app
//
// IMPORTANT NOTES:
// - Standard MAUI start-up
// =========================================================
[Application]
public sealed class MainApplication : MauiApplication
{
    public MainApplication(IntPtr handle, JniHandleOwnership ownership)
        : base(handle, ownership)
    {
    }

    protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
