using MySpawnerController.Shared;

namespace MySpawnerController.Api.Application;

public sealed class SpawnResult
{
    public int StatusCode { get; set; }
    public object Body { get; set; }

    public static SpawnResult Ok(object body) => new() { StatusCode = 200, Body = body };
    public static SpawnResult BadRequest(string msg) => new() { StatusCode = 400, Body = new ErrorResponse { Error = msg } };
    public static SpawnResult NotFound(string msg) => new() { StatusCode = 404, Body = new ErrorResponse { Error = msg } };
    public static SpawnResult NotReady() => new() { StatusCode = 503, Body = new ErrorResponse { Error = "Session not ready" } };
    public static SpawnResult Error(string msg) => new() { StatusCode = 500, Body = new ErrorResponse { Error = msg } };
}
