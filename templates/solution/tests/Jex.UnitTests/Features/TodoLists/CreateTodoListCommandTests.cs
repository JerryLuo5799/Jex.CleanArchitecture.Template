using Jex.Application.Common.Interfaces;
using Jex.Application.Features.TodoLists.Commands.CreateTodoList;
using Jex.Domain.Entities;

namespace Jex.UnitTests.Features.TodoLists;

public class CreateTodoListCommandTests
{
    private readonly Mock<ITodoListRepository> _repository = new();

    [Fact]
    public async Task Handle_AddsListWithDefaultColour_AndReturnsId()
    {
        _repository
            .Setup(r => r.AddAsync(It.IsAny<TodoList>(), It.IsAny<CancellationToken>()))
            .Callback<TodoList, CancellationToken>((list, _) => list.Id = 7)
            .ReturnsAsync((TodoList list, CancellationToken _) => list);

        var id = await new CreateTodoListCommandHandler(_repository.Object)
            .Handle(new CreateTodoListCommand("Shopping"), CancellationToken.None);

        id.ShouldBe(7);
        _repository.Verify(r => r.AddAsync(It.Is<TodoList>(l => l.Title == "Shopping" && l.Colour == "#FFFFFF"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Validate_DuplicateTitle_Fails()
    {
        _repository.Setup(r => r.IsTitleUniqueAsync("Taken", null, It.IsAny<CancellationToken>())).ReturnsAsync(false);
        var validator = new CreateTodoListCommandValidator(_repository.Object);

        var result = await validator.TestValidateAsync(new CreateTodoListCommand("Taken"));

        result.ShouldHaveValidationErrorFor(x => x.Title);
    }

    [Theory]
    [InlineData("#1E90FF", true)]
    [InlineData("red", false)]
    [InlineData("#12345", false)]
    public async Task Validate_Colour_MustBeHex(string colour, bool valid)
    {
        _repository.Setup(r => r.IsTitleUniqueAsync(It.IsAny<string>(), null, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        var validator = new CreateTodoListCommandValidator(_repository.Object);

        var result = await validator.TestValidateAsync(new CreateTodoListCommand("Colours", colour));

        if (valid)
        {
            result.ShouldNotHaveValidationErrorFor(x => x.Colour);
        }
        else
        {
            result.ShouldHaveValidationErrorFor(x => x.Colour);
        }
    }
}
