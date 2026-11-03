using Plugin.BLE;
using Vixly.PrintHelper.Core;
using ZXing.Net.Maui.Controls;

namespace Vixly.PrintHelper;

// =========================================================
// FILE: MauiProgram.cs
//
// PURPOSE:
// Builds the print helper app (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Register the camera reader, the Bluetooth printer, the settings store and the pages
//
// IMPORTANT NOTES:
// - One HttpClient for the whole app
// =========================================================
public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        MauiAppBuilder builder = MauiApp.CreateBuilder();
        builder.UseMauiApp<App>().UseBarcodeReader();
        builder.Services.AddSingleton(CrossBluetoothLE.Current);
        builder.Services.AddSingleton(CrossBluetoothLE.Current.Adapter);
        builder.Services.AddSingleton<IBluetoothPrinter, BlePrinter>();
        builder.Services.AddSingleton<ISettingsStore, SecureSettingsStore>();
        builder.Services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(20) });
        builder.Services.AddSingleton<PrintHelperClient>();
        builder.Services.AddSingleton<PrintLoop>();
        builder.Services.AddSingleton<MainPage>();
        return builder.Build();
    }
}
