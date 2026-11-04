using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.PrintOperationsShipmentLabels;

// =========================================================
// FILE: PrintOperationsShipmentLabelsResponse.cs
//
// PURPOSE:
// The labels of a shipment printed at once: the PDF, or how many went to the printer (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the PDF (when printed from the browser), the count printed and the count still waiting
//
// IMPORTANT NOTES:
// - Waiting: shipping labels of parcels not cleared yet (inspection, measures, a hold)
// =========================================================
public sealed class PrintOperationsShipmentLabelsResponse
{
    private PrintOperationsShipmentLabelsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[]? Content { get; private init; }

    public string FileName { get; private init; } = string.Empty;

    public int Printed { get; private init; }

    public int Waiting { get; private init; }

    public static PrintOperationsShipmentLabelsResponse Success(byte[]? content, string fileName, int printed, int waiting) =>
        new() { Succeeded = true, Content = content, FileName = fileName, Printed = printed, Waiting = waiting };

    public static PrintOperationsShipmentLabelsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new PrintOperationsShipmentLabelsResponse { Error = error };
    }
}
