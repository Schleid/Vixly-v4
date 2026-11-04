using System.Net.Sockets;
using System.Net;
using System.Text;

namespace Vixly.PrintAgent.Printing;

// =========================================================
// FILE: NetworkLabelPrinter.cs
//
// PURPOSE:
// Sends labels to a network label printer on its raw port (Slice 3B).
//
// RESPONSIBILITIES:
// - Open host:port, write the ZPL, close
//
// IMPORTANT NOTES:
// - Port 9100 is the usual raw port of Zebra and most thermal printers
// - Five seconds to connect: a printer that is off fails the job; the clerk scans again
// - Only addresses of the local network: a printer record never sends data out of the warehouse
// =========================================================
public sealed class NetworkLabelPrinter : ILabelPrinterPort
{
    public string Connection => "network";

    public async Task<string?> SendAsync(string address, string payload, CancellationToken cancellationToken)
    {
        int colon = address.LastIndexOf(':');

        if (colon <= 0 || !int.TryParse(address[(colon + 1)..], out int port))
        {
            return "address_invalid";
        }

        try
        {
            using CancellationTokenSource timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            IPAddress[] found = await Dns.GetHostAddressesAsync(address[..colon], timeout.Token);
            IPAddress? local = found.FirstOrDefault(Local);

            if (local is null)
            {
                return "address_not_local";
            }

            using TcpClient client = new();
            await client.ConnectAsync(local, port, timeout.Token);
            await using NetworkStream stream = client.GetStream();
            await stream.WriteAsync(Encoding.UTF8.GetBytes(payload), timeout.Token);
            await stream.FlushAsync(timeout.Token);
            return null;
        }
        catch (Exception exception) when (exception is SocketException or IOException or OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return "printer_unreachable";
        }
    }

    // Private ranges (10/8, 172.16/12, 192.168/16), link-local and unique-local IPv6, or this computer.
    private static bool Local(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.IsIPv4MappedToIPv6)
        {
            address = address.MapToIPv4();
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            return address.IsIPv6LinkLocal || address.IsIPv6UniqueLocal;
        }

        byte[] bytes = address.GetAddressBytes();
        return bytes[0] == 10 || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) || (bytes[0] == 192 && bytes[1] == 168) || (bytes[0] == 169 && bytes[1] == 254);
    }
}
