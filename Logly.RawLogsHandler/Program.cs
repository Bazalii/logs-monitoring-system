using Logly.RawLogsHandler.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddDependencies();

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.MapOpenApi();
app.UseSwaggerUI(
    swaggerUiOptions =>
    {
        swaggerUiOptions.SwaggerEndpoint("/openapi/v1.json", "RawLogsHandler v1");
    });

app.MapControllers();

app.Run("http://*:5001");