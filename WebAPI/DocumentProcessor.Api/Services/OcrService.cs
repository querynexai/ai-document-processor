using Tesseract;

namespace DocumentProcessor.Api.Services;

public class OcrService
{
    private readonly string _tessdataPath;

    public OcrService(IConfiguration config)
    {
        // Set this to the folder containing eng.traineddata
        _tessdataPath = config["Tesseract:DataPath"] ?? "./tessdata";
    }

    public string ExtractText(Stream imageStream)
    {
        using var engine = new TesseractEngine(_tessdataPath, "eng", EngineMode.Default);
        using var ms = new MemoryStream();
        imageStream.CopyTo(ms);
        using var pix = Pix.LoadFromMemory(ms.ToArray());
        using var page = engine.Process(pix);
        return page.GetText();
    }
}