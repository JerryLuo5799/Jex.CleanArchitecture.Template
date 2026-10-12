using Jex.Application.Common.Behaviours;
using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;

namespace Jex.UnitTests.Common.Behaviours;

public class UnitOfWorkBehaviourTests
{
    public sealed record SampleCommand : ICommand<string>;

    public sealed record SampleQuery : IQuery<string>;

    [NonTransactional]
    public sealed record SampleNonTransactionalCommand : ICommand<string>;

    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkScope> _scope = new();
    private readonly Mock<IDomainEventDispatcher> _events = new();

    public UnitOfWorkBehaviourTests()
    {
        _unitOfWork.Setup(u => u.Begin()).Returns(_scope.Object);
    }

    [Fact]
    public async Task Handle_Command_BeginsDispatchesAndCommits()
    {
        _scope.SetupGet(s => s.IsOwner).Returns(true);
        var behaviour = new UnitOfWorkBehaviour<SampleCommand, string>(_unitOfWork.Object, _events.Object);

        var result = await behaviour.Handle(new SampleCommand(), _ => Task.FromResult("ok"), CancellationToken.None);

        result.ShouldBe("ok");
        _unitOfWork.Verify(u => u.Begin(), Times.Once);
        _events.Verify(e => e.DispatchAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scope.Verify(s => s.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _scope.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task Handle_CommandThatThrows_DoesNotCommitAndDiscardsEvents()
    {
        _scope.SetupGet(s => s.IsOwner).Returns(true);
        var behaviour = new UnitOfWorkBehaviour<SampleCommand, string>(_unitOfWork.Object, _events.Object);

        await Should.ThrowAsync<InvalidOperationException>(
            () => behaviour.Handle(new SampleCommand(), _ => throw new InvalidOperationException("boom"), CancellationToken.None));

        _scope.Verify(s => s.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _events.Verify(e => e.DispatchAsync(It.IsAny<CancellationToken>()), Times.Never);
        _events.Verify(e => e.Discard(), Times.Once);
        _scope.Verify(s => s.Dispose(), Times.Once);
    }

    [Fact]
    public async Task Handle_NestedCommand_JoinsWithoutCommitting()
    {
        _scope.SetupGet(s => s.IsOwner).Returns(false);
        var behaviour = new UnitOfWorkBehaviour<SampleCommand, string>(_unitOfWork.Object, _events.Object);

        await behaviour.Handle(new SampleCommand(), _ => Task.FromResult("inner"), CancellationToken.None);

        _scope.Verify(s => s.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _events.Verify(e => e.DispatchAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Query_SkipsUnitOfWork()
    {
        var behaviour = new UnitOfWorkBehaviour<SampleQuery, string>(_unitOfWork.Object, _events.Object);

        await behaviour.Handle(new SampleQuery(), _ => Task.FromResult("read"), CancellationToken.None);

        _unitOfWork.Verify(u => u.Begin(), Times.Never);
    }

    [Fact]
    public async Task Handle_NonTransactionalCommand_SkipsUnitOfWork()
    {
        var behaviour = new UnitOfWorkBehaviour<SampleNonTransactionalCommand, string>(_unitOfWork.Object, _events.Object);

        await behaviour.Handle(new SampleNonTransactionalCommand(), _ => Task.FromResult("login"), CancellationToken.None);

        _unitOfWork.Verify(u => u.Begin(), Times.Never);
    }
}
