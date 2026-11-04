using System.Globalization;
using Vixly.Operations.Application.Ports;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsInvoicePdf.cs
//
// PURPOSE:
// Renders an invoice in its own language (Slice 5F, I-2).
//
// RESPONSIBILITIES:
// - Switch to the invoice's language while it is drawn
//
// IMPORTANT NOTES:
// - Staff and the customer get the same document
// =========================================================
public static class OperationsInvoicePdf
{
    public static byte[] Render(IOperationsLabelRenderer renderer, OperationsInvoiceDocument document)
    {
        ArgumentNullException.ThrowIfNull(renderer);
        ArgumentNullException.ThrowIfNull(document);
        CultureInfo before = CultureInfo.CurrentUICulture;

        try
        {
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(document.Invoice.Language);
            return renderer.RenderInvoice(document);
        }
        finally
        {
            CultureInfo.CurrentUICulture = before;
        }
    }
}
