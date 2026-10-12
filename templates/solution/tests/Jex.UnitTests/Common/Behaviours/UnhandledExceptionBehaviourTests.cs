using Jex.Application.Common.Behaviours;
using Jex.Application.Common.Exceptions;

namespace Jex.UnitTests.Common.Behaviours;

public class UnhandledExceptionBehaviourTests
{
    public sealed record Ping : IRequest<string>;

    private readonly Mock<ILogger<Ping>> _logger = new();

    [Fact]
    public async Task Handle_UnexpectedException_LogsErrorAndRethrows()
    {
        _logger.Setup(l => l.IsEnabled(LogLevel.Error)).Returns(true);
        var behaviour = new UnhandledExceptionBehaviour<Ping, string>(_logger.Object);

        await Should.ThrowAsync<InvalidOperationException>(
            () => behaviour.Handle(new Ping(), _ => throw new InvalidOperationException("boom"), CancellationToken.None));

        _logger.Verify(
            l => l.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<InvalidOperationException>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_KnownException_IsNotLogged()
    {
        var behaviour = new UnhandledExceptionBehaviour<Ping, string>(_logger.Object);

        await Should.ThrowAsync<NotFoundException>(
            () => behaviour.Handle(new Ping(), _ => throw new NotFoundException("Thing", 1), CancellationToken.None));

        _logger.Verify(
            l => l.Log(It.IsAny<LogLevel>(), It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }
}
