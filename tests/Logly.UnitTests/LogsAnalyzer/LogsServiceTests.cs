using Logly.LogsAnalyzer.Domain.Models.Core.Logs;
using Logly.LogsAnalyzer.Domain.Repositories.Logs;
using Logly.LogsAnalyzer.Domain.Services.Logs;
using NSubstitute;

namespace Logly.UnitTests.LogsAnalyzer;

public class LogsServiceTests
{
    [Fact]
    public async Task SearchAsync_DelegatesToRepository()
    {
        // Arrange
        var repo = Substitute.For<ILogsRepository>();
        var sut = new LogsService(repo);

        var query = new LogsSearchQuery();
        using var cts = new CancellationTokenSource();

        var expected = new LogsSearchResult(123, new List<ILog>());
        repo.SearchAsync(query, cts.Token).Returns(expected);

        // Act
        var result = await sut.SearchAsync(query, cts.Token);

        // Assert
        Assert.Same(expected, result);
        await repo.Received(1).SearchAsync(query, cts.Token);
    }
}