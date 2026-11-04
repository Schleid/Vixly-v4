using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Domain.Settings;

// =========================================================
// FILE: OperationsNumberSequence.cs
//
// PURPOSE:
// The last number a tenant issued for one kind in one year (D-O12).
//
// RESPONSIBILITIES:
// - Hold the counter row; allocation happens in the database, atomically
//
// IMPORTANT NOTES:
// - Never decreases; gaps are possible when a command fails after taking a number
// =========================================================
public sealed class OperationsNumberSequence : OperationsEntity
{
    private OperationsNumberSequence()
    {
    }

    public OperationsNumberKind Kind { get; private set; }

    public int Year { get; private set; }

    public long LastNumber { get; private set; }
}
