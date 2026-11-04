using Vixly.Contracts.Operations.Printing;
using Vixly.PrintHelper.Core;
using ZXing.Net.Maui;
using ZXing.Net.Maui.Controls;

namespace Vixly.PrintHelper;

// =========================================================
// FILE: MainPage.cs
//
// PURPOSE:
// The print helper's one page (Slice 12E, owner H-6): join a warehouse, choose the printer, then print.
//
// RESPONSIBILITIES:
// - Not joined: scan the QR code of the Printers page (or type the address and code)
// - Joined: find the Bluetooth printers nearby, choose one, and print every few seconds while open
//
// IMPORTANT NOTES:
// - Built in code, no XAML
// - A stopped phone forgets its key and asks to join again
// =========================================================
public sealed class MainPage : ContentPage
{
    private readonly ISettingsStore _store;

    private readonly PrintHelperClient _client;

    private readonly PrintLoop _loop;

    private readonly IBluetoothPrinter _bluetooth;

    private readonly VerticalStackLayout _content = new() { Padding = 20, Spacing = 12 };

    private PrintHelperSettings _settings = PrintHelperSettings.Empty;

    private IDispatcherTimer? _timer;

    private int _printed;

    private int _failed;

    private bool _running;

    public MainPage(ISettingsStore store, PrintHelperClient client, PrintLoop loop, IBluetoothPrinter bluetooth)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _loop = loop ?? throw new ArgumentNullException(nameof(loop));
        _bluetooth = bluetooth ?? throw new ArgumentNullException(nameof(bluetooth));
        Title = AppInfo.Current.Name;
        Content = new ScrollView { Content = _content };
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _settings = await _store.LoadAsync();
        Show();
    }

    protected override void OnDisappearing()
    {
        _timer?.Stop();
        base.OnDisappearing();
    }

    private void Show()
    {
        _content.Children.Clear();

        if (!_settings.IsJoined)
        {
            ShowJoin();
        }
        else if (!_settings.HasPrinter)
        {
            ShowChoose();
        }
        else
        {
            ShowPrinting();
        }
    }

    private void ShowJoin()
    {
        _timer?.Stop();
        Entry address = new() { Placeholder = Texts.Get("address"), Keyboard = Keyboard.Url };
        Entry code = new() { Placeholder = Texts.Get("code") };
        Label failed = new() { TextColor = Colors.DarkRed, IsVisible = false, Text = Texts.Get("join_failed") };
        Button scan = new() { Text = Texts.Get("scan") };
        Button join = new() { Text = Texts.Get("join_button") };
        scan.Clicked += async (_, _) => await ScanAsync(failed);
        join.Clicked += async (_, _) => await JoinAsync(PairingLink.FromTyped(address.Text, code.Text), failed);
        _content.Children.Add(new Label { Text = Texts.Get("join"), FontSize = 18 });
        _content.Children.Add(scan);
        _content.Children.Add(new Label { Text = Texts.Get("or_type") });
        _content.Children.Add(address);
        _content.Children.Add(code);
        _content.Children.Add(join);
        _content.Children.Add(failed);
    }

    private async Task ScanAsync(Label failed)
    {
        TaskCompletionSource<string?> read = new();
        CameraBarcodeReaderView camera = new()
        {
            Options = new BarcodeReaderOptions { Formats = BarcodeFormat.QrCode, AutoRotate = true, Multiple = false }
        };
        camera.BarcodesDetected += (_, found) => read.TrySetResult(found.Results.FirstOrDefault()?.Value);
        ContentPage page = new() { Content = camera };
        await Navigation.PushModalAsync(page);
        string? scanned = await read.Task;
        await MainThread.InvokeOnMainThreadAsync(() => Navigation.PopModalAsync());
        await JoinAsync(PairingLink.FromScan(scanned), failed);
    }

    private async Task JoinAsync(PairingLink? link, Label failed)
    {
        PhonePairDto? joined = link is null ? null : await _client.PairAsync(link, DeviceInfo.Current.Name, CancellationToken.None);

        if (joined is null)
        {
            failed.IsVisible = true;
            return;
        }

        _settings = PrintHelperSettings.Empty with { Site = link!.Site, AgentKey = joined.Key, Warehouse = joined.Warehouse };
        await _store.SaveAsync(_settings);
        Show();
    }

    private void ShowChoose()
    {
        CheckBox shipping = new();
        VerticalStackLayout found = new() { Spacing = 8 };
        Button find = new() { Text = Texts.Get("find") };
        find.Clicked += async (_, _) =>
        {
            found.Children.Clear();
            IReadOnlyList<BluetoothPrinterInfo> printers = await _bluetooth.ScanAsync(TimeSpan.FromSeconds(8), CancellationToken.None);

            if (printers.Count == 0)
            {
                found.Children.Add(new Label { Text = Texts.Get("none_found") });
            }

            foreach (BluetoothPrinterInfo printer in printers)
            {
                Button choose = new() { Text = printer.Name };
                choose.Clicked += async (_, _) =>
                {
                    _settings = _settings with { PrinterName = printer.Name, PrinterAddress = printer.Address, Media = shipping.IsChecked ? "shipping" : "id" };

                    if (await _client.SavePrinterAsync(_settings, true, CancellationToken.None) == System.Net.HttpStatusCode.OK)
                    {
                        await _store.SaveAsync(_settings);
                        Show();
                    }
                };
                found.Children.Add(choose);
            }
        };
        _content.Children.Add(new Label { Text = Texts.Get("joined", _settings.Warehouse ?? string.Empty), FontSize = 18 });
        _content.Children.Add(new Label { Text = Texts.Get("choose") });
        _content.Children.Add(new HorizontalStackLayout { Spacing = 8, Children = { shipping, new Label { Text = Texts.Get("shipping"), VerticalOptions = LayoutOptions.Center } } });
        _content.Children.Add(find);
        _content.Children.Add(found);
        AddLeave();
    }

    private void ShowPrinting()
    {
        Label status = new() { Text = Texts.Get("status", _printed, _failed) };
        _content.Children.Add(new Label { Text = Texts.Get("joined", _settings.Warehouse ?? string.Empty), FontSize = 18 });
        _content.Children.Add(new Label { Text = Texts.Get("printer", _settings.PrinterName ?? string.Empty) });
        _content.Children.Add(status);
        _content.Children.Add(new Label { Text = Texts.Get("keep_open"), FontSize = 12 });
        AddLeave();
        DeviceDisplay.Current.KeepScreenOn = true;
        _timer?.Stop();
        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(3);
        _timer.Tick += async (_, _) =>
        {
            if (_running)
            {
                return;
            }

            _running = true;

            try
            {
                PrintRound round = await _loop.RunOnceAsync(_settings, CancellationToken.None);

                if (round.Stopped)
                {
                    _settings = PrintHelperSettings.Empty;
                    await _store.SaveAsync(_settings);
                    await DisplayAlert(AppInfo.Current.Name, Texts.Get("stopped"), "OK");
                    Show();
                    return;
                }

                (_printed, _failed) = (_printed + round.Printed, _failed + round.Failed);
                status.Text = Texts.Get("status", _printed, _failed);
            }
            catch (HttpRequestException)
            {
                // No signal: the next round tries again.
            }
            finally
            {
                _running = false;
            }
        };
        _timer.Start();
    }

    private void AddLeave()
    {
        Button leave = new() { Text = Texts.Get("leave"), BackgroundColor = Colors.Transparent, TextColor = Colors.DarkRed };
        leave.Clicked += async (_, _) =>
        {
            _timer?.Stop();
            DeviceDisplay.Current.KeepScreenOn = false;
            _settings = PrintHelperSettings.Empty;
            await _store.SaveAsync(_settings);
            Show();
        };
        _content.Children.Add(leave);
    }
}
