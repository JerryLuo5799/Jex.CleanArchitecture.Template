using Jex.Application.Common.Interfaces;

namespace Jex.Application.Features.TodoLists.Commands.CreateTodoList;

/// <summary>Validators may take dependencies: here the repository checks that the title is unique among active lists.</summary>
public sealed class CreateTodoListCommandValidator : AbstractValidator<CreateTodoListCommand>
{
    public CreateTodoListCommandValidator(ITodoListRepository repository)
    {
        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .MustAsync((title, ct) => repository.IsTitleUniqueAsync(title, cancellationToken: ct))
            .WithMessage("'{PropertyName}' must be unique.");

        RuleFor(x => x.Colour)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .When(x => x.Colour is not null)
            .WithMessage("'{PropertyName}' must be a hex colour like #1E90FF.");
    }
}
