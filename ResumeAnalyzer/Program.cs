using ResumeAnalyzer.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorPages();
builder.Services.AddSingleton<ResumeAnalyzerService>();

var app = builder.Build();
app.UseStaticFiles();
app.MapRazorPages();
app.Run();
