using System.Net;
using System.Text;
using System.Text.Json;
using Vixly.PrintHelper.Core;

namespace Vixly.PrintHelper.Core.Tests;

// =========================================================
// FILE: PrintHelperCoreTests.cs
//
// PURPOSE:
// The print helper's logic (Slice 12E, owner H-6) without a phone.
//
// RESPONSIBILITIES:
// - Read the QR code's link and a typed code
// - Print the waiting labels on the Bluetooth printer and report them; skip old ones; notice a stopped phone
//
// IMPORTANT NOTES:
// - A fake Operations (HTTP handler) and a fake printer
// =========================================================
public sealed class PrintHelperCoreTests
{
    private static readonly PrintHelperSettings Joined = new(new Uri("https://hdl.example.test/"), "vxpa.key", "Port-au-Prince", "ZQ520", "AA:BB:CC:DD:EE:FF", 203, "id");

    [Fact]
    public void ScannedLink_GivesTheSiteAndTheCode_OnlyOverHttps()
    {
        PairingLink link = PairingLink.FromScan("https://hdl.example.test/agent/v1/pair?code=ABCD-EFGH-JKLM")!;

        Assert.Equal(new Uri("https://hdl.example.test/"), link.Site);
        Assert.Equal("ABCD-EFGH-JKLM", link.Code);
        Assert.Null(PairingLink.FromScan("http://hdl.example.test/agent/v1/pair?code=ABCD-EFGH-JKLM"));
        Assert.Null(PairingLink.FromScan("https://hdl.example.test/somewhere?code=ABCD-EFGH-JKLM"));
        Assert.Null(PairingLink.FromScan("not a link"));
        Assert.Equal(new Uri("https://hdl.example.test/"), PairingLink.FromTyped("hdl.example.test", "abcd-efgh-jklm")!.Site);
        Assert.Null(PairingLink.FromTyped("hdl.example.test", " "));
    }

    [Fact]
    public async Task Round_PrintsOnTheBluetoothPrinter_ReportsEachLabel_AndSkipsOldOnes()
    {
        Guid fresh = Guid.NewGuid(), old = Guid.NewGuid(), other = Guid.NewGuid();
        FakeOperations operations = new(HttpStatusCode.OK, new object[]
        {
            new { jobId = fresh, connection = "bluetooth", address = "AA:BB:CC:DD:EE:FF", payload = "^XA^FDPK1^FS^XZ", expiresInSeconds = 60 },
            new { jobId = old, connection = "bluetooth", address = "AA:BB:CC:DD:EE:FF", payload = "^XA^XZ", expiresInSeconds = 0 },
            new { jobId = other, connection = "network", address = "10.0.0.5:9100", payload = "^XA^XZ", expiresInSeconds = 60 }
        });
        FakePrinter printer = new();

        PrintRound round = await new PrintLoop(new PrintHelperClient(new HttpClient(operations)), printer).RunOnceAsync(Joined, CancellationToken.None);

        Assert.Equal((1, 2, false), (round.Printed, round.Failed, round.Stopped));
        Assert.Equal("^XA^FDPK1^FS^XZ", Assert.Single(printer.Sent).Payload);
        Assert.Contains(operations.Reports, report => report.Contains(fresh.ToString(), StringComparison.Ordinal) && report.Contains("\"printed\":true", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(operations.Reports, report => report.Contains(old.ToString(), StringComparison.Ordinal) && report.Contains("expired", StringComparison.Ordinal));
        Assert.Contains(operations.Reports, report => report.Contains(other.ToString(), StringComparison.Ordinal) && report.Contains("connection_unknown", StringComparison.Ordinal));
        Assert.All(operations.Authorizations, value => Assert.Equal("Bearer vxpa.key", value));
    }

    [Fact]
    public async Task Round_NoticesThePhoneWasStopped_AndDoesNothingWithoutAPrinter()
    {
        FakePrinter printer = new();
        PrintRound stopped = await new PrintLoop(new PrintHelperClient(new HttpClient(new FakeOperations(HttpStatusCode.Unauthorized, Array.Empty<object>()))), printer)
            .RunOnceAsync(Joined, CancellationToken.None);
        PrintRound none = await new PrintLoop(new PrintHelperClient(new HttpClient(new FakeOperations(HttpStatusCode.OK, Array.Empty<object>()))), printer)
            .RunOnceAsync(Joined with { PrinterAddress = null }, CancellationToken.None);

        Assert.True(stopped.Stopped);
        Assert.Equal(new PrintRound(0, 0, false), none);
        Assert.Empty(printer.Sent);
    }

    private sealed class FakePrinter : IBluetoothPrinter
    {
        public List<(string Address, string Payload)> Sent { get; } = new();

        public Task<IReadOnlyList<BluetoothPrinterInfo>> ScanAsync(TimeSpan duration, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<BluetoothPrinterInfo>>(Array.Empty<BluetoothPrinterInfo>());

        public Task<string?> SendAsync(string address, string payload, CancellationToken cancellationToken)
        {
            Sent.Add((address, payload));
            return Task.FromResult<string?>(null);
        }
    }

    private sealed class FakeOperations : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;

        private readonly object[] _jobs;

        public FakeOperations(HttpStatusCode status, object[] jobs)
        {
            (_status, _jobs) = (status, jobs);
        }

        public List<string> Reports { get; } = new();

        public List<string?> Authorizations { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorizations.Add(request.Headers.Authorization?.ToString());

            if (request.Method == HttpMethod.Get)
            {
                return new HttpResponseMessage(_status) { Content = new StringContent(JsonSerializer.Serialize(_jobs), Encoding.UTF8, "application/json") };
            }

            Reports.Add(request.RequestUri!.AbsolutePath + " " + await request.Content!.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(HttpStatusCode.NoContent);
        }
    }
}
