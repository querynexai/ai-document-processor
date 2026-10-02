using Microsoft.AspNetCore.Mvc;
using DocumentProcessor.Api.Models;
using DocumentProcessor.Api.Services;

namespace DocumentProcessor.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly PdfExtractionService _pdf;
    private readonly OcrService _ocr;
    private readonly InvoiceExtractionService _extraction;
    private readonly InvoiceService _invoices;

    public DocumentsController(
        PdfExtractionService pdf,
        OcrService ocr,
        InvoiceExtractionService extraction,
        InvoiceService invoices)
    {
        _pdf = pdf;
        _ocr = ocr;
        _extraction = extraction;
        _invoices = invoices;
    }

    [HttpGet]
    public Task<List<InvoiceSummaryDto>> List([FromQuery] string? status) =>
        _invoices.ListAsync(status);

    [HttpGet("{id}")]
    public async Task<ActionResult<InvoiceDetailDto>> Get(int id)
    {
        var result = await _invoices.GetAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpGet("stats")]
    public Task<DashboardStats> Stats() => _invoices.GetStatsAsync();

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInvoiceRequest req)
    {
        var ok = await _invoices.UpdateAsync(id, req);
        return ok ? NoContent() : NotFound();
    }

    [HttpPost("upload")]
    [RequestSizeLimit(20_000_000)] // 20 MB
    public async Task<ActionResult<ProcessingResult>> Upload(IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new ProcessingResult(0, false, null, "No file uploaded."));

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            string rawText;

            if (file.ContentType == "application/pdf" || file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                await using var stream = file.OpenReadStream();
                rawText = _pdf.ExtractText(stream);
            }
            else if (file.ContentType.StartsWith("image/"))
            {
                await using var stream = file.OpenReadStream();
                rawText = _ocr.ExtractText(stream);
            }
            else
            {
                return BadRequest(new ProcessingResult(0, false, null, $"Unsupported file type: {file.ContentType}"));
            }

            if (string.IsNullOrWhiteSpace(rawText))
                return BadRequest(new ProcessingResult(0, false, null, "Could not extract any text from the document."));

            var extraction = await _extraction.ExtractAsync(rawText);
            sw.Stop();

            var invoiceId = await _invoices.SaveExtractionAsync(
                file.FileName,
                file.Length,
                file.ContentType,
                rawText,
                extraction,
                (int)sw.ElapsedMilliseconds);

            var confidence = extraction?["confidence"]?.GetValue<decimal>();
            return Ok(new ProcessingResult(invoiceId, true, confidence));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ProcessingResult(0, false, null, ex.Message));
        }
    }
}