using System.Globalization;
using System.Text;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Legal;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformDemoLegal.cs
//
// PURPOSE:
// Demo data for Slice 8: published Privacy Policy, Data Processing Agreement and Terms of Service.
//
// RESPONSIBILITIES:
// - Publish version "2026-10" of each, with a small sample PDF in English, French and Spanish
// - The Terms need acceptance, so demo tenants are due in 30 days and the reminder clock has work
//
// IMPORTANT NOTES:
// - Development only; off unless PlatformDemoData:SeedOnStartup is true
// - Safe to run again: nothing is added once a Terms version exists
// - Labels match the configured registration versions, so registration keeps working
// =========================================================
public sealed class PlatformDemoLegal
{
    public const string Version = "2026-10";

    private readonly IPlatformLegalRepository _legal;

    private readonly IPlatformTenantQueries _tenants;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _clock;

    public PlatformDemoLegal(IPlatformLegalRepository legal, IPlatformTenantQueries tenants, IPlatformUnitOfWork unitOfWork, TimeProvider clock)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _clock = clock ?? throw new ArgumentNullException(nameof(clock));
    }

    public async Task<string> SeedAsync(CancellationToken cancellationToken)
    {
        if (await _legal.VersionExistsAsync(PlatformLegalDocumentKind.Terms, Version, null, cancellationToken))
        {
            return "Demo legal: documents already present.";
        }

        IReadOnlyList<PlatformStaffOptionItem> staff = await _tenants.ListActiveStaffAsync(cancellationToken);

        if (staff.Count == 0)
        {
            return "Demo legal: create and sign in the first SuperUser first.";
        }

        Guid actor = staff[0].PlatformUserId;
        DateTimeOffset now = _clock.GetUtcNow();
        List<PlatformLegalDocument> parts = new();

        foreach (PlatformLegalDocumentKind kind in new[] { PlatformLegalDocumentKind.Privacy, PlatformLegalDocumentKind.DataProcessing, PlatformLegalDocumentKind.Terms })
        {
            PlatformLegalDocument document = PlatformLegalDocument.CreateDraft(kind, Version, actor, now);

            foreach (LanguageCode language in LanguageCode.All)
            {
                _legal.AddFile(PlatformLegalDocumentFile.Create(document.Id, language.Value, Pdf(kind + " " + Version + " (" + language.Value + ") - sample text for development"), Summary(language.Value), now));
            }

            if (kind == PlatformLegalDocumentKind.Terms)
            {
                document.SetParts(parts, now);
            }

            document.Publish(LanguageCode.All.Select(language => language.Value), parts.Select(part => part.Kind), actor, now);
            _legal.AddDocument(document);
            parts.Add(document);
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? "Demo legal: published Privacy, DPA and Terms " + Version + "."
            : "Demo legal: documents could not be saved.";
    }

    private static string Summary(string language) => language switch
    {
        "fr" => "Première version publiée dans Vixly V4.",
        "es" => "Primera versión publicada en Vixly V4.",
        _ => "First version published in Vixly V4."
    };

    // A one-page PDF with a line of text and a correct cross-reference table.
    private static byte[] Pdf(string text)
    {
        string safe = text.Replace("\\", "\\\\", StringComparison.Ordinal).Replace("(", "\\(", StringComparison.Ordinal).Replace(")", "\\)", StringComparison.Ordinal);
        string stream = "BT /F1 14 Tf 72 720 Td (" + safe + ") Tj ET";
        string[] objects =
        {
            "<< /Type /Catalog /Pages 2 0 R >>",
            "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R /Resources << /Font << /F1 5 0 R >> >> >>",
            "<< /Length " + Encoding.ASCII.GetByteCount(stream).ToString(CultureInfo.InvariantCulture) + " >>\nstream\n" + stream + "\nendstream",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>"
        };

        StringBuilder pdf = new("%PDF-1.4\n");
        List<int> offsets = new();

        for (int index = 0; index < objects.Length; index++)
        {
            offsets.Add(Encoding.ASCII.GetByteCount(pdf.ToString()));
            pdf.Append(CultureInfo.InvariantCulture, $"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
        }

        int xref = Encoding.ASCII.GetByteCount(pdf.ToString());
        pdf.Append(CultureInfo.InvariantCulture, $"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");

        foreach (int offset in offsets)
        {
            pdf.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
        }

        pdf.Append(CultureInfo.InvariantCulture, $"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return Encoding.ASCII.GetBytes(pdf.ToString());
    }
}
