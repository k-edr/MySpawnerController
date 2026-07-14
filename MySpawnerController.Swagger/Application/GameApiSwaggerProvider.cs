using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;

namespace MySpawnerController.Swagger.Application;

/// <summary>
/// Serves a pre-built <see cref="OpenApiDocument"/> to Swashbuckle middleware.
/// </summary>
public sealed class GameApiSwaggerProvider(OpenApiDocument doc) : ISwaggerProvider
{
    public OpenApiDocument GetSwagger(string documentName, string host = null!, string basePath = null!) => doc;
}
