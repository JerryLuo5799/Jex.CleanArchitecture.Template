using Jex.Application.Common.Behaviours;
using Jex.Application.Common.Interfaces;

namespace Jex.UnitTests.Common.Behaviours;

public class PerformanceBehaviourTests
{
    public sealed record Ping : IRequest<string>;

    private readonly Mock<ILogger<Ping>> _logger = new();
    private readonly Mock<ICurrentUser> _currentUser = new();

    [Fact]
    public async Task Handle_FastRequest_DoesNotWarn()
    {
        _logger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);
        var behaviour = new PerformanceBehaviour<Ping, string>(_logger.Object, _currentUser.Object);

        var result = await behaviour.Handle(new Ping(), _ => Task.FromResult("pong"), CancellationToken.None);

        result.ShouldBe("pong");
        _logger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SlowRequest_Warns()
    {
        _logger.Setup(l => l.IsEnabled(LogLevel.Warning)).Returns(true);
        var behaviour = new PerformanceBehaviour<Ping, string>(_logger.Object, _currentUser.Object);

        await behaviour.Handle(
            new Ping(),
            async ct =>
            {
                await Task.Delay(PerformanceBehaviour<Ping, string>.ThresholdMilliseconds + 50, ct);
                return "pong";
            },
            CancellationToken.None);

        _logger.Verify(
            l => l.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }
}
