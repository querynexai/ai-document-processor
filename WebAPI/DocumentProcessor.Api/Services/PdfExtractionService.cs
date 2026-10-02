using UglyToad.PdfPig;

namespace DocumentProcessor.Api.Services;

public class PdfExtractionService
{
    public string ExtractText(Stream pdfStream)
    {
        var sb = new System.Text.StringBuilder();
        using var document = PdfDocument.Open(pdfStream);
        foreach (var page in document.GetPages())
        {
            sb.AppendLine(page.Text);
            sb.AppendLine();
        }
        return sb.ToString();
    }
}