using System.Text;
using Plugin.BLE.Abstractions.Contracts;
using Plugin.BLE.Abstractions.EventArgs;
using Vixly.PrintHelper.Core;

namespace Vixly.PrintHelper;

// =========================================================
// FILE: BlePrinter.cs
//
// PURPOSE:
// Sends labels to a portable printer over Bluetooth Low Energy (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - List the printers nearby that offer the label service
// - Connect, write the ZPL in small pieces, disconnect
//
// IMPORTANT NOTES:
// - Zebra Link-OS printers (ZQ, ZD) offer BLE with this service; iPhones cannot use classic Bluetooth without Apple's MFi program
// - The address is the phone's id of the printer (Plugin.BLE device id)
// =========================================================
public sealed class BlePrinter : IBluetoothPrinter
{
    // Zebra's "Parser" service and its write characteristic (Link-OS BLE).
    private static readonly Guid Service = Guid.Parse("38eb4a80-c570-11e3-9507-0002a5d5c51b");

    private static readonly Guid Write = Guid.Parse("38eb4a82-c570-11e3-9507-0002a5d5c51b");

    private readonly IAdapter _adapter;

    public BlePrinter(IAdapter adapter)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
    }

    public async Task<IReadOnlyList<BluetoothPrinterInfo>> ScanAsync(TimeSpan duration, CancellationToken cancellationToken)
    {
        if (!await AllowedAsync())
        {
            return Array.Empty<BluetoothPrinterInfo>();
        }

        List<BluetoothPrinterInfo> found = new();
        void Seen(object? sender, DeviceEventArgs args)
        {
            if (!string.IsNullOrWhiteSpace(args.Device.Name) && found.All(item => item.Address != args.Device.Id.ToString()))
            {
                found.Add(new BluetoothPrinterInfo(args.Device.Name, args.Device.Id.ToString()));
            }
        }

        _adapter.DeviceDiscovered += Seen;
        _adapter.ScanTimeout = (int)duration.TotalMilliseconds;

        try
        {
            await _adapter.StartScanningForDevicesAsync(new ScanFilterOptions { ServiceUuids = new[] { Service } }, cancellationToken: cancellationToken);
        }
        finally
        {
            _adapter.DeviceDiscovered -= Seen;
        }

        return found;
    }

    public async Task<string?> SendAsync(string address, string payload, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(address, out Guid id))
        {
            return "address_invalid";
        }

        IDevice? device = null;

        try
        {
            device = await _adapter.ConnectToKnownDeviceAsync(id, cancellationToken: cancellationToken);
            IService? service = await device.GetServiceAsync(Service, cancellationToken);
            ICharacteristic? write = service is null ? null : await service.GetCharacteristicAsync(Write);

            if (write is null)
            {
                return "not_a_label_printer";
            }

            byte[] bytes = Encoding.UTF8.GetBytes(payload);

            // Small pieces: a BLE write carries about 20 bytes unless a larger size was agreed.
            for (int offset = 0; offset < bytes.Length; offset += 180)
            {
                await write.WriteAsync(bytes.AsSpan(offset, Math.Min(180, bytes.Length - offset)).ToArray(), cancellationToken);
            }

            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return "printer_unreachable";
        }
        finally
        {
            if (device is not null)
            {
                await _adapter.DisconnectDeviceAsync(device);
            }
        }
    }

    private static async Task<bool> AllowedAsync() =>
        await Permissions.RequestAsync<Permissions.Bluetooth>() == PermissionStatus.Granted;
}
