using JexSolution.Application.Features.JexFeature.Queries.JexUseCase;

namespace JexSolution.UnitTests.Features.JexFeature;

/// <summary>Scaffolded test: replace with real cases once the handler is implemented (AGENTS.md section 9).</summary>
public class JexUseCaseQueryTests
{
    [Fact]
    public async Task Handle_ValidQuery_Completes()
    {
        var handler = new JexUseCaseQueryHandler();

#if (WithId)
        await Should.NotThrowAsync(() => handler.Handle(new JexUseCaseQuery(1), CancellationToken.None));
#else
        await Should.NotThrowAsync(() => handler.Handle(new JexUseCaseQuery(), CancellationToken.None));
#endif
    }
}
