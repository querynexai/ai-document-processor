using Npgsql;
using System.Text.Json.Nodes;
using DocumentProcessor.Api.Models;

namespace DocumentProcessor.Api.Services;

public class InvoiceService
{
    private readonly NpgsqlDataSource _dataSource;

    public InvoiceService(NpgsqlDataSource dataSource) => _dataSource = dataSource;

    public async Task<int> SaveExtractionAsync(
        string filename, long fileSize, string mimeType,
        string rawText, JsonNode? extraction, int processingMs)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();

        var docType = extraction?["documentType"]?.GetValue<string>();
        var vendor = extraction?["vendorName"]?.GetValue<string>();
        var invNum = extraction?["invoiceNumber"]?.GetValue<string>();
        var invDate = ParseDate(extraction?["invoiceDate"]?.GetValue<string>());
        var dueDate = ParseDate(extraction?["dueDate"]?.GetValue<string>());
        var subtotal = ParseDecimal(extraction?["subtotal"]);
        var tax = ParseDecimal(extraction?["taxAmount"]);
        var total = ParseDecimal(extraction?["totalAmount"]);
        var currency = extraction?["currency"]?.GetValue<string>() ?? "INR";
        var confidence = extraction?["confidence"]?.GetValue<decimal>() ?? 0;
        var status = confidence < 0.75m ? "needs_review" : "processed";

        int invoiceId;
        await using (var cmd = new NpgsqlCommand(
            @"INSERT INTO invoices
              (filename, file_size, mime_type, document_type, raw_text,
               vendor_name, invoice_number, invoice_date, due_date,
               subtotal, tax_amount, total_amount, currency,
               status, confidence_score, ai_model, processing_time_ms, processed_at)
              VALUES (@fn, @fs, @mt, @dt, @rt, @vn, @in, @id, @dd,
                      @st, @ta, @tot, @cur, @st2, @cs, @ai, @pt, NOW())
              RETURNING invoice_id", conn))
        {
            cmd.Parameters.AddWithValue("fn", filename);
            cmd.Parameters.AddWithValue("fs", fileSize);
            cmd.Parameters.AddWithValue("mt", (object?)mimeType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("dt", (object?)docType ?? DBNull.Value);
            cmd.Parameters.AddWithValue("rt", rawText);
            cmd.Parameters.AddWithValue("vn", (object?)vendor ?? DBNull.Value);
            cmd.Parameters.AddWithValue("in", (object?)invNum ?? DBNull.Value);
            cmd.Parameters.AddWithValue("id", (object?)invDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("dd", (object?)dueDate ?? DBNull.Value);
            cmd.Parameters.AddWithValue("st", (object?)subtotal ?? DBNull.Value);
            cmd.Parameters.AddWithValue("ta", (object?)tax ?? DBNull.Value);
            cmd.Parameters.AddWithValue("tot", (object?)total ?? DBNull.Value);
            cmd.Parameters.AddWithValue("cur", currency);
            cmd.Parameters.AddWithValue("st2", status);
            cmd.Parameters.AddWithValue("cs", confidence);
            cmd.Parameters.AddWithValue("ai", "groq/gpt-oss-120b");
            cmd.Parameters.AddWithValue("pt", processingMs);

            invoiceId = (int)(await cmd.ExecuteScalarAsync())!;
        }

        // Line items
        var lineItems = extraction?["lineItems"]?.AsArray();
        if (lineItems is not null)
        {
            foreach (var item in lineItems)
            {
                if (item is null) continue;

                await using var cmd = new NpgsqlCommand(
                    @"INSERT INTO invoice_line_items
                      (invoice_id, line_number, description, quantity, unit_price, total_price, confidence_score)
                      VALUES (@iid, @ln, @d, @q, @up, @tp, @cs)", conn);

                cmd.Parameters.AddWithValue("iid", invoiceId);
                cmd.Parameters.AddWithValue("ln", item["lineNumber"]?.GetValue<int>() ?? 0);
                cmd.Parameters.AddWithValue("d", (object?)item["description"]?.GetValue<string>() ?? DBNull.Value);
                cmd.Parameters.AddWithValue("q", (object?)ParseDecimal(item["quantity"]) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("up", (object?)ParseDecimal(item["unitPrice"]) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("tp", (object?)ParseDecimal(item["totalPrice"]) ?? DBNull.Value);
                cmd.Parameters.AddWithValue("cs", item["confidence"]?.GetValue<decimal>() ?? 0);

                await cmd.ExecuteNonQueryAsync();
            }
        }

        return invoiceId;
    }

    public async Task<List<InvoiceSummaryDto>> ListAsync(string? status = null)
    {
        var results = new List<InvoiceSummaryDto>();
        await using var conn = await _dataSource.OpenConnectionAsync();

        var sql = @"SELECT invoice_id, filename, document_type, vendor_name, invoice_number,
                           invoice_date, total_amount, status, confidence_score, uploaded_at
                    FROM invoices";
        if (!string.IsNullOrEmpty(status)) sql += " WHERE status = @st";
        sql += " ORDER BY uploaded_at DESC";

        await using var cmd = new NpgsqlCommand(sql, conn);
        if (!string.IsNullOrEmpty(status)) cmd.Parameters.AddWithValue("st", status);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new InvoiceSummaryDto(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.IsDBNull(2) ? null : reader.GetString(2),
                reader.IsDBNull(3) ? null : reader.GetString(3),
                reader.IsDBNull(4) ? null : reader.GetString(4),
                reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                reader.GetString(7),
                reader.IsDBNull(8) ? null : reader.GetDecimal(8),
                reader.GetDateTime(9)));
        }
        return results;
    }

    public async Task<InvoiceDetailDto?> GetAsync(int id)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();

        await using var cmd = new NpgsqlCommand(
            @"SELECT invoice_id, filename, document_type, vendor_name, invoice_number,
                     invoice_date, due_date, subtotal, tax_amount, total_amount,
                     currency, status, confidence_score, raw_text, uploaded_at, processed_at
              FROM invoices WHERE invoice_id = @id", conn);
        cmd.Parameters.AddWithValue("id", id);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return null;

        var invoice = new InvoiceDetailDto(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.IsDBNull(4) ? null : reader.GetString(4),
            reader.IsDBNull(5) ? null : reader.GetDateTime(5),
            reader.IsDBNull(6) ? null : reader.GetDateTime(6),
            reader.IsDBNull(7) ? null : reader.GetDecimal(7),
            reader.IsDBNull(8) ? null : reader.GetDecimal(8),
            reader.IsDBNull(9) ? null : reader.GetDecimal(9),
            reader.IsDBNull(10) ? null : reader.GetString(10),
            reader.GetString(11),
            reader.IsDBNull(12) ? null : reader.GetDecimal(12),
            reader.IsDBNull(13) ? null : reader.GetString(13),
            new List<LineItemDto>(),                    // moved here
            reader.GetDateTime(14),
            reader.IsDBNull(15) ? null : reader.GetDateTime(15)
        );

        await reader.CloseAsync();

        // Load line items
        var items = new List<LineItemDto>();
        await using var itemCmd = new NpgsqlCommand(
            @"SELECT line_item_id, line_number, description, quantity, unit_price, total_price, confidence_score
              FROM invoice_line_items WHERE invoice_id = @id ORDER BY line_number", conn);
        itemCmd.Parameters.AddWithValue("id", id);

        await using var itemReader = await itemCmd.ExecuteReaderAsync();
        while (await itemReader.ReadAsync())
        {
            items.Add(new LineItemDto(
                itemReader.GetInt32(0),
                itemReader.GetInt32(1),
                itemReader.IsDBNull(2) ? null : itemReader.GetString(2),
                itemReader.IsDBNull(3) ? null : itemReader.GetDecimal(3),
                itemReader.IsDBNull(4) ? null : itemReader.GetDecimal(4),
                itemReader.IsDBNull(5) ? null : itemReader.GetDecimal(5),
                itemReader.IsDBNull(6) ? null : itemReader.GetDecimal(6)));
        }

        return invoice with { LineItems = items };
    }

    public async Task<bool> UpdateAsync(int id, UpdateInvoiceRequest req)
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            @"UPDATE invoices SET
            vendor_name = COALESCE(@vn, vendor_name),
            invoice_number = COALESCE(@invNum, invoice_number),
            invoice_date = COALESCE(@invDate, invoice_date),
            due_date = COALESCE(@dueDate, due_date),
            subtotal = COALESCE(@subtotal, subtotal),
            tax_amount = COALESCE(@tax, tax_amount),
            total_amount = COALESCE(@total, total_amount),
            currency = COALESCE(@currency, currency),
            notes = COALESCE(@notes, notes),
            status = 'approved',
            reviewed_at = NOW()
          WHERE invoice_id = @invoiceId", conn);

        cmd.Parameters.AddWithValue("invoiceId", id);
        cmd.Parameters.AddWithValue("vn", (object?)req.VendorName ?? DBNull.Value);
        cmd.Parameters.AddWithValue("invNum", (object?)req.InvoiceNumber ?? DBNull.Value);
        cmd.Parameters.AddWithValue("invDate", (object?)req.InvoiceDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("dueDate", (object?)req.DueDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("subtotal", (object?)req.Subtotal ?? DBNull.Value);
        cmd.Parameters.AddWithValue("tax", (object?)req.TaxAmount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("total", (object?)req.TotalAmount ?? DBNull.Value);
        cmd.Parameters.AddWithValue("currency", (object?)req.Currency ?? DBNull.Value);
        cmd.Parameters.AddWithValue("notes", (object?)req.Notes ?? DBNull.Value);

        return await cmd.ExecuteNonQueryAsync() > 0;
    }

    public async Task<DashboardStats> GetStatsAsync()
    {
        await using var conn = await _dataSource.OpenConnectionAsync();
        await using var cmd = new NpgsqlCommand(
            @"SELECT
                COUNT(*),
                COUNT(*) FILTER (WHERE status = 'needs_review' OR status = 'pending'),
                COUNT(*) FILTER (WHERE status = 'approved'),
                COALESCE(AVG(confidence_score), 0),
                COALESCE(SUM(total_amount), 0)
              FROM invoices", conn);

        await using var reader = await cmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync()) return new DashboardStats(0, 0, 0, 0, 0);

        return new DashboardStats(
            (int)reader.GetInt64(0),
            (int)reader.GetInt64(1),
            (int)reader.GetInt64(2),
            reader.GetDecimal(3),
            reader.GetDecimal(4));
    }

    private static DateTime? ParseDate(string? s) =>
        DateTime.TryParse(s, out var d) ? d : null;

    private static decimal? ParseDecimal(JsonNode? node)
    {
        if (node is null) return null;
        try { return node.GetValue<decimal>(); } catch { return null; }
    }
}