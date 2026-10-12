using JexSolution.Application.Features.JexFeature.Commands.JexUseCase;

namespace JexSolution.UnitTests.Features.JexFeature;

/// <summary>Scaffolded tests: replace with real cases once the handler is implemented (AGENTS.md section 9).</summary>
public class JexUseCaseCommandTests
{
    [Fact]
    public void Validator_ValidCommand_HasNoErrors()
    {
#if (WithId)
        var result = new JexUseCaseCommandValidator().TestValidate(new JexUseCaseCommand(1));
#else
        var result = new JexUseCaseCommandValidator().TestValidate(new JexUseCaseCommand());
#endif

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Handle_ValidCommand_Completes()
    {
        var handler = new JexUseCaseCommandHandler();

#if (WithId)
        await Should.NotThrowAsync(() => handler.Handle(new JexUseCaseCommand(1), CancellationToken.None));
#else
        await Should.NotThrowAsync(() => handler.Handle(new JexUseCaseCommand(), CancellationToken.None));
#endif
    }
}
