namespace DocumentProcessor.Api.Models;

public record InvoiceSummaryDto(
    int InvoiceId,
    string Filename,
    string? DocumentType,
    string? VendorName,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    decimal? TotalAmount,
    string Status,
    decimal? ConfidenceScore,
    DateTime UploadedAt);

public record InvoiceDetailDto(
    int InvoiceId,
    string Filename,
    string? DocumentType,
    string? VendorName,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    DateTime? DueDate,
    decimal? Subtotal,
    decimal? TaxAmount,
    decimal? TotalAmount,
    string? Currency,
    string Status,
    decimal? ConfidenceScore,
    string? RawText,
    List<LineItemDto> LineItems,
    DateTime UploadedAt,
    DateTime? ProcessedAt);

public record LineItemDto(
    int LineItemId,
    int LineNumber,
    string? Description,
    decimal? Quantity,
    decimal? UnitPrice,
    decimal? TotalPrice,
    decimal? ConfidenceScore);

public record UpdateInvoiceRequest(
    string? VendorName,
    string? InvoiceNumber,
    DateTime? InvoiceDate,
    DateTime? DueDate,
    decimal? Subtotal,
    decimal? TaxAmount,
    decimal? TotalAmount,
    string? Currency,
    string? Notes);

public record ProcessingResult(
    int InvoiceId,
    bool Success,
    decimal? OverallConfidence,
    string? Error = null);

public record DashboardStats(
    int TotalInvoices,
    int PendingReview,
    int Approved,
    decimal AverageConfidence,
    decimal TotalValue);