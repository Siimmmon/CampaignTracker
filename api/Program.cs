using CampaignTracker.Api.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var connectionString = builder.Configuration.GetConnectionString("CampaignDb");
if (string.IsNullOrWhiteSpace(connectionString))
    throw new InvalidOperationException("Configure ConnectionStrings:CampaignDb before starting the app.");

var useSqlite = builder.Environment.IsDevelopment();
builder.Services.AddDbContext<CampaignDbContext>(options =>
{
    if (useSqlite)
        options.UseSqlite(connectionString);
    else
        options.UseSqlServer(connectionString);
});
builder.Services.AddControllers();
builder.Services.AddProblemDetails();

var app = builder.Build();
app.UseExceptionHandler();

if (useSqlite)
{
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<CampaignDbContext>();
    await database.Database.EnsureCreatedAsync();
}

var serveFrontend = app.Environment.WebRootFileProvider.GetFileInfo("index.html").Exists;
if (serveFrontend)
    app.UseStaticFiles();

app.MapControllers();
app.MapFallback("/api/{**path}", () => Results.Problem(statusCode: StatusCodes.Status404NotFound));
if (serveFrontend)
    app.MapFallbackToFile("index.html");

await app.RunAsync();
