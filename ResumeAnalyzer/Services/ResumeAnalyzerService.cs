using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ResumeAnalyzer.Services;

public record AnalysisResult(
    int Score, int SkillScore, int StructureScore,
    List<string> FoundSkills, List<string> MissingSkills,
    List<string> Suggestions, string Email, string Phone, int WordCount);

public class ResumeAnalyzerService
{
    private static readonly string[] SkillBank =
    {
        "c#", ".net", "asp.net", "entity framework", "linq", "sql", "sql server", "mysql",
        "javascript", "typescript", "html", "css", "react", "angular", "blazor",
        "python", "java", "azure", "docker", "git", "rest api", "web api", "microservices",
        "unit testing", "agile", "machine learning", "oop", "design patterns", "ado.net"
    };

    // ---------- Text extraction ----------
    public string ExtractText(Stream stream, string fileName)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        switch (ext)
        {
            case ".txt":
                using (var reader = new StreamReader(stream)) return reader.ReadToEnd();

            case ".docx":
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Read))
                {
                    var entry = zip.GetEntry("word/document.xml")
                        ?? throw new InvalidDataException("Invalid .docx file.");
                    using var es = entry.Open();
                    var xml = XDocument.Load(es);
                    XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
                    var paragraphs = xml.Descendants(w + "p")
                        .Select(p => string.Concat(p.Descendants(w + "t").Select(t => t.Value)));
                    return string.Join("\n", paragraphs);
                }

            case ".pdf":
                throw new NotSupportedException("PDF is not supported in this offline build. Please save your resume as .docx or .txt.");

            default:
                throw new NotSupportedException("Please upload a .docx or .txt file.");
        }
    }

    // ---------- Analysis ----------
    public AnalysisResult Analyze(string resume, string? jobDescription)
    {
        // If a job description is given, only skills it mentions are "required"
        var required = string.IsNullOrWhiteSpace(jobDescription)
            ? SkillBank.ToList()
            : SkillBank.Where(s => HasSkill(jobDescription, s)).ToList();
        if (required.Count == 0) required = SkillBank.ToList();

        var found = required.Where(s => HasSkill(resume, s)).ToList();
        var missing = required.Except(found).ToList();
        int skillScore = (int)Math.Round(100.0 * found.Count / required.Count);

        // Structure checks
        var email = Regex.Match(resume, @"[\w.+-]+@[\w-]+\.[\w.]+").Value;
        var phone = Regex.Match(resume, @"(\+?\d[\d\s\-]{8,14}\d)").Value;
        int words = Regex.Matches(resume, @"\b\w+\b").Count;

        var suggestions = new List<string>();
        int structure = 0;

        void Check(bool ok, int pts, string tip)
        {
            if (ok) structure += pts; else suggestions.Add(tip);
        }

        Check(email != "", 15, "Add a professional email address.");
        Check(phone != "", 10, "Add a contact phone number.");
        Check(Has(resume, "education"), 15, "Add an Education section.");
        Check(Has(resume, "experience") || Has(resume, "internship"), 15, "Add an Experience or Internship section.");
        Check(Has(resume, "skills"), 15, "Add a dedicated Skills section.");
        Check(Has(resume, "project"), 15, "List your academic or personal Projects.");
        Check(words is >= 200 and <= 900, 15, words < 200
            ? "Resume looks too short - add more detail (aim for 300-600 words)."
            : "Resume looks too long - keep it concise (1-2 pages).");

        if (missing.Count > 0)
            suggestions.Add("Consider learning/adding: " + string.Join(", ", missing.Take(6)) + ".");

        int total = (int)Math.Round(skillScore * 0.7 + structure * 0.3);
        return new AnalysisResult(total, skillScore, structure, found, missing, suggestions, email, phone.Trim(), words);
    }

    private static bool Has(string text, string word) =>
        text.Contains(word, StringComparison.OrdinalIgnoreCase);

    private static bool HasSkill(string text, string skill) =>
        Regex.IsMatch(text, $@"(?<![A-Za-z0-9]){Regex.Escape(skill)}(?![A-Za-z0-9])", RegexOptions.IgnoreCase);
}
