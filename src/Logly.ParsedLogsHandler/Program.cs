using Logly.ParsedLogsHandler.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddDependencies();

var app = builder.Build();

app.MapHealthChecks("/healthz");

app.Run("http://*:5002");