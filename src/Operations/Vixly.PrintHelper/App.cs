namespace Vixly.PrintHelper;

// =========================================================
// FILE: App.cs
//
// PURPOSE:
// The print helper app (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Open the main page in a window
//
// IMPORTANT NOTES:
// - No other page: joining, choosing the printer and printing all happen on it
// =========================================================
public sealed class App : Application
{
    private readonly MainPage _main;

    public App(MainPage main)
    {
        _main = main ?? throw new ArgumentNullException(nameof(main));
    }

    protected override Window CreateWindow(IActivationState? activationState) => new(new NavigationPage(_main));
}
