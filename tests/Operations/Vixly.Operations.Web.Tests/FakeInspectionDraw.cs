using Vixly.Operations.Application.Receiving;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: FakeInspectionDraw.cs
//
// PURPOSE:
// Stands in for the random compliance draw (D-R4), so a test decides which parcel is picked.
//
// RESPONSIBILITIES:
// - Answer what the test set
//
// IMPORTANT NOTES:
// - Tests only; picks nothing by default
// =========================================================
internal sealed class FakeInspectionDraw : OperationsInspectionDraw
{
    public bool Picks { get; set; }

    public int? LastPercent { get; private set; }

    public override bool Selects(int percent)
    {
        LastPercent = percent;
        return Picks;
    }
}
