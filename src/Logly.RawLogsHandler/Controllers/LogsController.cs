using Logly.RawLogsHandler.Domain.Services.Logs;
using Logly.RawLogsHandler.Integration.Models.Requests;
using Microsoft.AspNetCore.Mvc;

namespace Logly.RawLogsHandler.Controllers;

[ApiController]
[Route("logs")]
public class LogsController(
    ILogsService logsService)
    : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult> SendLogs(
        string rawLog,
        CancellationToken cancellation)
    {
        await logsService.SendRawLogAsync(rawLog, cancellation);

        return Ok();
    }

    [HttpPost("parse")]
    public async Task<ActionResult> ParseLogs(
        ParseLogsRequest request,
        CancellationToken cancellation)
    {
        await logsService.SendLogsFromFileAsync(request.FilePath, cancellation);

        return Ok();
    }
}