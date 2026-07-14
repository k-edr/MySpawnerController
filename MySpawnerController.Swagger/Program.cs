using Microsoft.Extensions.Logging;
using MySpawnerController.Swagger.Application;
using MySpawnerController.Swagger.Infrastructure;

const int SwaggerPort = 9998;
const int GamePort = 9997;

var doc = OpenApiDocumentBuilder.Build(GamePort);

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseUrls($"http://localhost:{SwaggerPort}");
builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Services.AddSwaggerDocument(doc);

var app = builder.Build();

app.ConfigureSwagger();
SwaggerHostExtensions.PrintBanner(SwaggerPort);

app.Run();
