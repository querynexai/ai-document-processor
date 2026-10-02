using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DocumentProcessor.Api.Services;

public class InvoiceExtractionService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly string _model;

    public InvoiceExtractionService(HttpClient http, IConfiguration config)
    {
        _http = http;
        _apiKey = config["Groq:ApiKey"] ?? throw new InvalidOperationException("Groq:ApiKey not set");
        _model = config["Groq:Model"] ?? "openai/gpt-oss-120b";
        _http.BaseAddress = new Uri("https://api.groq.com/openai/v1/");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
    }

    public async Task<JsonNode?> ExtractAsync(string rawText)
    {
        var systemPrompt = """
            You are a document extraction AI. Given raw text from an invoice or receipt, extract the following fields.

            Return ONLY valid JSON matching this exact schema. No explanation, no markdown fences.

            {
              "documentType": "invoice" | "purchase_order" | "receipt" | "contract" | "unknown",
              "vendorName": "string or null",
              "invoiceNumber": "string or null",
              "invoiceDate": "YYYY-MM-DD or null",
              "dueDate": "YYYY-MM-DD or null",
              "subtotal": number or null,
              "taxAmount": number or null,
              "totalAmount": number or null,
              "currency": "3-letter code, default INR",
              "confidence": 0.0 to 1.0,
              "lineItems": [
                {
                  "lineNumber": 1,
                  "description": "string",
                  "quantity": number or null,
                  "unitPrice": number or null,
                  "totalPrice": number or null,
                  "confidence": 0.0 to 1.0
                }
              ]
            }

            Rules:
            - If a field is not present, use null.
            - Never invent values.
            - Confidence should reflect your certainty of the extraction (0.9 = very sure, 0.5 = uncertain).
            - Extract line items when present.
            - Amounts must be numeric (no currency symbols).
            """;

        var payload = new
        {
            model = _model,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"Extract from this document:\n\n{rawText}" }
            },
            temperature = 0.1,
            response_format = new { type = "json_object" }
        };

        var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
        var response = await _http.PostAsync("chat/completions", content);
        var body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Groq error ({response.StatusCode}): {body}");

        using var doc = JsonDocument.Parse(body);
        var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()
            ?? "{}";

        text = text.Replace("```json", "").Replace("```", "").Trim();

        return JsonNode.Parse(text);
    }
}