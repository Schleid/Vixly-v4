namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsInsuranceExclusions.cs
//
// PURPOSE:
// The standard exclusions of a courier or forwarder insurance, each on or off (Slice 4C section 8).
//
// RESPONSIBILITIES:
// - Name the usual exclusions; the company adds its own as text
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// - Shown to the customer in their language (OperationsText and CustomerText)
// =========================================================
[Flags]
public enum OperationsInsuranceExclusions
{
    None = 0,

    CashAndDocuments = 1,

    JewelryAndPreciousMetals = 2,

    Perishables = 4,

    LiveAnimals = 8,

    HazardousGoods = 16,

    FragileNotPackedByUs = 32,

    UsedElectronicsWithoutProof = 64,

    ImproperPacking = 128,

    CustomsSeizure = 256,

    Standard = CashAndDocuments | JewelryAndPreciousMetals | Perishables | LiveAnimals | HazardousGoods | FragileNotPackedByUs | UsedElectronicsWithoutProof | ImproperPacking | CustomsSeizure
}
