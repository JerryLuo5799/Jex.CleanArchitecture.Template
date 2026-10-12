using Jex.Application.Common.Interfaces;

namespace Jex.Application.Features.TodoLists.Commands.UpdateTodoList;

public sealed class UpdateTodoListCommandValidator : AbstractValidator<UpdateTodoListCommand>
{
    public UpdateTodoListCommandValidator(ITodoListRepository repository)
    {
        RuleFor(x => x.Id).GreaterThan(0);

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(200)
            .MustAsync((command, title, ct) => repository.IsTitleUniqueAsync(title, command.Id, ct))
            .WithMessage("'{PropertyName}' must be unique.");

        RuleFor(x => x.Colour)
            .Matches("^#[0-9A-Fa-f]{6}$")
            .When(x => x.Colour is not null)
            .WithMessage("'{PropertyName}' must be a hex colour like #1E90FF.");
    }
}
