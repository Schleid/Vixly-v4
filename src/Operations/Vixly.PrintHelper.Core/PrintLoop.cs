using System.Diagnostics;
using System.Net;
using Vixly.Contracts.Operations.Printing;

namespace Vixly.PrintHelper.Core;

// =========================================================
// FILE: PrintLoop.cs
//
// PURPOSE:
// One round of the print helper (Slice 12E, owner H-6): take the waiting labels of this phone's printer and print them.
//
// RESPONSIBILITIES:
// - Take the jobs, print each on the printer next to the phone, report the result
// - Say when the phone was stopped on the Printers page
//
// IMPORTANT NOTES:
// - A label no longer fresh is not printed (the clerk has moved on)
// - The app runs a round every few seconds while it is open
// =========================================================
public sealed class PrintLoop
{
    private readonly PrintHelperClient _client;

    private readonly IBluetoothPrinter _printer;

    public PrintLoop(PrintHelperClient client, IBluetoothPrinter printer)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _printer = printer ?? throw new ArgumentNullException(nameof(printer));
    }

    public async Task<PrintRound> RunOnceAsync(PrintHelperSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!settings.HasPrinter)
        {
            return new PrintRound(0, 0, false);
        }

        (HttpStatusCode status, IReadOnlyList<PrintJobDto> jobs) = await _client.TakeJobsAsync(settings, cancellationToken);

        if (status == HttpStatusCode.Unauthorized)
        {
            return new PrintRound(0, 0, true);
        }

        Stopwatch received = Stopwatch.StartNew();
        int printed = 0, failed = 0;

        foreach (PrintJobDto job in jobs)
        {
            string? error;

            try
            {
                error = job.Connection != "bluetooth" ? "connection_unknown"
                    : received.Elapsed.TotalSeconds >= job.ExpiresInSeconds ? "expired"
                    : await _printer.SendAsync(job.Address, job.Payload, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                error = "print_failed";
            }

            _ = error is null ? printed++ : failed++;

            try
            {
                await _client.ReportAsync(settings, job.JobId, error, cancellationToken);
            }
            catch (HttpRequestException)
            {
                // Only for the record: a lost report never holds up the next label.
            }
        }

        return new PrintRound(printed, failed, false);
    }
}
