using DocImageSort.Api.Data;
using DocImageSort.Api.Services;
using DocImageSort.Api.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.PropertyNameCaseInsensitive = true);
builder.Services.AddOpenApi();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddHostedService<DropFolderWatcherService>();
builder.Services.AddScoped<IDocumentPipelineService, DocumentPipelineService>();
builder.Services.AddScoped<IClassificationService, ClassificationService>();
builder.Services.AddScoped<IConversionService, ConversionService>();
builder.Services.AddScoped<IRenameService, RenameService>();
builder.Services.AddScoped<IRoutingService, RoutingService>();
builder.Services.AddScoped<IDocumentReviewService, DocumentReviewService>();
builder.Services.AddScoped<IDuplicateDetectionService, DuplicateDetectionService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReact", policy =>
        policy.WithOrigins("http://localhost:5173")
              .AllowAnyHeader()
              .AllowAnyMethod());
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Ensure required directories exist before the file watcher starts.
var dropPath  = app.Configuration["DropFolder:Path"]  ?? @"C:\DocImageSort\Drop";
var filesPath = app.Configuration["FilesFolder:Path"] ?? @"C:\DocImageSort\Files";
if (!Directory.Exists(dropPath))  Directory.CreateDirectory(dropPath);
if (!Directory.Exists(filesPath)) Directory.CreateDirectory(filesPath);

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    // Safe column additions for databases created before these columns existed.
    try { db.Database.ExecuteSqlRaw("ALTER TABLE \"Documents\" ADD COLUMN \"FileHash\" TEXT NOT NULL DEFAULT ''"); }
    catch { /* column already exists */ }

    try { db.Database.ExecuteSqlRaw("ALTER TABLE \"Documents\" ADD COLUMN \"DocumentQualifier\" TEXT NOT NULL DEFAULT ''"); }
    catch { /* column already exists */ }

    // Seed users (Phase 1: no passwords — auth added in Phase 2).
    if (!db.Users.Any())
    {
        db.Users.AddRange(
            new DocImageSort.Api.Models.User
            {
                FirstName = "Matt",
                LastName  = "Mahan",
                Email     = "mahanster@gmail.com",
                Username  = "mmahan",
                Role      = "Admin",
                IsActive  = true,
                CreatedBy = "system",
                UpdatedBy = "system"
            },
            new DocImageSort.Api.Models.User
            {
                FirstName = "Chance",
                LastName  = "Nelson",
                Email     = "chance.nelson@midwestbankers.com",
                Username  = "cnelson",
                Role      = "Admin",
                IsActive  = true,
                CreatedBy = "system",
                UpdatedBy = "system"
            }
        );
        db.SaveChanges();
    }
}

app.UseCors("AllowReact");
app.UseHttpsRedirection();
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseAuthorization();
app.MapControllers();
app.MapFallbackToFile("index.html");

app.Run();
