using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ResumeAnalyzer.Services;

namespace ResumeAnalyzer.Pages;

public class IndexModel : PageModel
{
    private readonly ResumeAnalyzerService _service;
    public IndexModel(ResumeAnalyzerService service) => _service = service;

    [BindProperty] public IFormFile? Resume { get; set; }
    [BindProperty] public string? JobDescription { get; set; }
    public AnalysisResult? Result { get; set; }
    public string? Error { get; set; }

    public void OnPost()
    {
        if (Resume == null || Resume.Length == 0)
        {
            Error = "Please choose a resume file.";
            return;
        }
        try
        {
            using var stream = new MemoryStream();
            Resume.CopyTo(stream);
            stream.Position = 0;
            var text = _service.ExtractText(stream, Resume.FileName);
            Result = _service.Analyze(text, JobDescription);
        }
        catch (Exception ex)
        {
            Error = ex.Message;
        }
    }
}
