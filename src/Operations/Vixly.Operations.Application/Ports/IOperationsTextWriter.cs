using Vixly.Operations.Domain.Texts;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsTextWriter.cs
//
// PURPOSE:
// Words a notice as a short text in the customer's language (Slice 9).
//
// RESPONSIBILITIES:
// - The sentence of a notice with its values (shipment, place, code), without the company or the link
//
// IMPORTANT NOTES:
// - The company's name only, never "Vixly"
// =========================================================
public interface IOperationsTextWriter
{
    string Sentence(OperationsTextNotices notice, LanguageCode language, IReadOnlyDictionary<string, string> values);
}
