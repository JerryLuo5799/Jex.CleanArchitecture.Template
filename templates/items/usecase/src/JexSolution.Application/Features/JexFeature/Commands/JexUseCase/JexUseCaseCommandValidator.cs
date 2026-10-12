namespace JexSolution.Application.Features.JexFeature.Commands.JexUseCase;

/// <summary>Every command has exactly one validator (enforced by the architecture tests). Add rules here, not in the handler.</summary>
public sealed class JexUseCaseCommandValidator : AbstractValidator<JexUseCaseCommand>
{
    public JexUseCaseCommandValidator()
    {
#if (WithId)
        RuleFor(x => x.Id).GreaterThan(0);
#else
        // TODO: RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
#endif
    }
}
