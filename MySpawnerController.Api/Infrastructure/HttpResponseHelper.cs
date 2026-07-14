using System.Net;
using System.Text;
using System.Text.Json;

namespace MySpawnerController.Api.Infrastructure;

/// <summary>
/// HTTP response helpers for <see cref="HttpListenerContext"/>.
/// </summary>
public static class HttpResponseHelper
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public static void Json(HttpListenerContext ctx, int code, object obj)
    {
        var json = JsonSerializer.Serialize(obj, JsonOpts);
        Respond(ctx, code, json, "application/json");
    }

    public static void Text(HttpListenerContext ctx, int code, string body)
    {
        Respond(ctx, code, body, "text/plain; charset=utf-8");
    }

    private static void Respond(HttpListenerContext ctx, int code, string body, string contentType)
    {
        try
        {
            ctx.Response.AddHeader("Access-Control-Allow-Origin", "http://localhost:9998");
            ctx.Response.AddHeader("Access-Control-Allow-Methods", "GET, POST, DELETE, OPTIONS");
            ctx.Response.AddHeader("Access-Control-Allow-Headers", "Content-Type");

            byte[] data = Encoding.UTF8.GetBytes(body);
            ctx.Response.StatusCode = code;
            ctx.Response.ContentType = contentType;
            ctx.Response.ContentLength64 = data.Length;
            ctx.Response.OutputStream.Write(data, 0, data.Length);
            ctx.Response.OutputStream.Close();
        }
        catch { }
    }
}
