using System.Text.Json;
using Microsoft.Extensions.Logging;
using GridSpawner.Shared.Configuration;
using GridSpawner.Swagger.Application;
using GridSpawner.Swagger.Infrastructure;

var config = LoadConfig();
var doc = OpenApiDocumentBuilder.Build(config);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"{config.ApiScheme}://{config.DisplayHost}:{config.SwaggerPort}");
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Services.AddSwaggerDocument(doc);

var app = builder.Build();

app.ConfigureSwagger(config);
SwaggerHostExtensions.PrintBanner(config);

app.Run();

static AppConfig LoadConfig()
{
    var path = AppDefaults.DefaultConfigFile;

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
