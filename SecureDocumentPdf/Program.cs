using Serilog;
using Serilog.Events;
using SecureDocumentPdf.Services;
using SecureDocumentPdf.Services.Interface;
using SecureDocumentPdf.Services;
using Microsoft.AspNetCore.StaticFiles;
using QuestPDF.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Configure QuestPDF license AVANT tout appel à QuestPDF
QuestPDF.Settings.License = LicenseType.Community;

// ========================================
// CONFIGURATION DE SERILOG (LOGGING)
// ========================================
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("System", LogEventLevel.Warning)
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/app-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

// Attacher Serilog à l'application
builder.Host.UseSerilog();

Log.Information("========================================");
Log.Information("Application PDF Security démarrée");
Log.Information("========================================");

// ========================================
// CONFIGURATION DES SERVICES
// ========================================

// Services Razor Pages
builder.Services.AddRazorPages();

// Enregistrer IHttpContextAccessor AVANT les services qui en dépendent
builder.Services.AddHttpContextAccessor();

builder.Services.AddControllers();

// Service personnalisé de traitement PDF
builder.Services.AddScoped<IPdfSecurityService, PdfSecurityService>();

// Configuration de la taille maximale des uploads (50MB)
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 52428800; // 50MB
});

// ========================================
// CONSTRUCTION DE L'APPLICATION
// ========================================
var app = builder.Build();

// ========================================
// CONFIGURATION DU PIPELINE HTTP
// ========================================

// Gestion des erreurs selon l'environnement
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    Log.Information("Mode : Production");
}
else
{
    app.UseDeveloperExceptionPage();
    Log.Information("Mode : Development");
}

var provider = new FileExtensionContentTypeProvider();

provider.Mappings[".js"] = "application/javascript";       // JS
provider.Mappings[".css"] = "text/css";                    // CSS
provider.Mappings[".map"] = "application/json";            // source maps si présentes
provider.Mappings[".woff2"] = "font/woff2";               // fonts
provider.Mappings[".woff"] = "font/woff";
provider.Mappings[".ttf"] = "font/ttf";
provider.Mappings[".eot"] = "application/vnd.ms-fontobject";
provider.Mappings[".svg"] = "image/svg+xml";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider,
    ServeUnknownFileTypes = true,      // sert tous les fichiers même si type inconnu
    DefaultContentType = "application/octet-stream"
});


//app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

// Mapping des Razor Pages
app.MapRazorPages();

// ========================================
// CRÉATION DES DOSSIERS NÉCESSAIRES
// ========================================
try
{
    var webRootPath = app.Environment.WebRootPath;

    var uploadsPath = Path.Combine(webRootPath, "uploads");
    var securedPath = Path.Combine(webRootPath, "secured");
    var securedPreviewsPath = Path.Combine(securedPath, "previews");

    Directory.CreateDirectory(uploadsPath);
    Directory.CreateDirectory(securedPath);
    Directory.CreateDirectory(securedPreviewsPath);

    Log.Information("Dossiers créés avec succès");
    Log.Information("  - Uploads : {UploadsPath}", uploadsPath);
    Log.Information("  - Secured : {SecuredPath}", securedPath);
    Log.Information("  - Previews : {PreviewsPath}", securedPreviewsPath);
}
catch (Exception ex)
{
    Log.Error(ex, "Erreur lors de la création des dossiers");
}

// ========================================
// AFFICHAGE DES INFORMATIONS DE DÉMARRAGE
// ========================================
Log.Information("========================================");
Log.Information("Application prête !");
Log.Information("URL : https://localhost:{Port}", app.Urls.FirstOrDefault() ?? "7001");
Log.Information("Environment : {Environment}", app.Environment.EnvironmentName);
Log.Information("========================================");

// ========================================
// DÉMARRAGE DE L'APPLICATION
// ========================================
try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "L'application s'est arrêtée de manière inattendue");
}
finally
{
    Log.Information("Application arrêtée");
    Log.CloseAndFlush();
}