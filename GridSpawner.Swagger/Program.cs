using System.Text.Json;
using Microsoft.Extensions.Logging;
using GridSpawner.Shared.Configuration;
using GridSpawner.Shared.Models;
using GridSpawner.Swagger.Application;
using GridSpawner.Swagger.Infrastructure;

var config = LoadConfig();
var doc = OpenApiDocumentBuilder.Build(config.ApiPort);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://localhost:{config.SwaggerPort}");
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Services.AddSwaggerDocument(doc);

var app = builder.Build();

app.ConfigureSwagger(config.ApiPort);
SwaggerHostExtensions.PrintBanner(config);

app.Run();

static AppConfig LoadConfig()
{
    var path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SpaceEngineers", "GridSpawner.json");

    try
    {
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<AppConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new AppConfig();
        }
    }
    catch { }

    return new AppConfig();
}
