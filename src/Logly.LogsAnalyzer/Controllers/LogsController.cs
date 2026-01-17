using Logly.LogsAnalyzer.Domain.Models.Core.Logs;
using Microsoft.AspNetCore.Mvc;
using Logly.LogsAnalyzer.Domain.Services.Logs;
using Logly.LogsAnalyzer.Integration.Models.Requests;
using Logly.LogsAnalyzer.Integration.Models.Responses;

namespace Logly.LogsAnalyzer.Controllers;

[ApiController]
[Route("logs")]
public class LogsController(ILogsService logsService) : ControllerBase
{
    [HttpGet("search")]
    public async Task<ActionResult<LogsSearchResponse>> SearchAsync(
        [FromQuery] LogsSearchRequest request,
        CancellationToken cancellation)
    {
        var query = new LogsSearchQuery
        {
            Level = request.Level,
            Source = request.Source,
            From = request.From,
            To = request.To,
            Limit = request.Limit,
            Offset = request.Offset,
        };

        var result = await logsService.SearchAsync(query, cancellation);

        var response = new LogsSearchResponse(
            result.Total,
            result.Logs.Select(LogResponse.FromDomain).ToArray());

        return Ok(response);
    }
}