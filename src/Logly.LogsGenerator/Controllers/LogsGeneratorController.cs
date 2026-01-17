using Logly.LogsGenerator.Domain.Services;
using Logly.LogsGenerator.Integration.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Logly.LogsGenerator.Controllers;

[ApiController]
[Route("logs")]
public class LogsGeneratorController(
    ILogsGeneratorService logsGeneratorService)
    : ControllerBase
{
    [HttpPost("kafka")]
    public async Task<ActionResult> SendLogsToKafkaAsync(
        GenerateKafkaLogsRequest request,
        CancellationToken cancellation)
    {
        var numberOfLogs = request.NumberOfLogs;

        switch (numberOfLogs)
        {
            case <= 0:
                return BadRequest("number_of_logs must be > 0");
            case > 1_000_000:
                return BadRequest("number_of_logs is too large (max 1_000_000)");
        }

        var format = request.Format.Trim().ToLowerInvariant();

        if (format != "json" && format != "syslog" && format != "clf" && format != "slf")
        {
            return BadRequest("format must be one of: json, syslog, clf");
        }

        await logsGeneratorService.SendLogsToKafkaAsync(
            numberOfLogs, request.Period, format, cancellation);

        return Ok();
    }
}