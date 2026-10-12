using Jex.Application.Common.Interfaces;
using Jex.Application.Features.Users.Queries.GetUsers;
using Jex.Domain.Entities;

namespace Jex.UnitTests.Features.Users.Queries;

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock = new();
    private readonly GetUsersQueryHandler _handler;

    public GetUsersQueryHandlerTests()
    {
        _handler = new GetUsersQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ReturnsPagedUserDtos()
    {
        var users = new List<User>
        {
            new() { Id = 1, FirstName = "Alice", LastName = "A", Email = "alice@example.com" },
            new() { Id = 2, FirstName = "Bob", LastName = "B", Email = "bob@example.com" },
        };
        _repositoryMock.Setup(r => r.GetPagedAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync(users);

        var result = await _handler.Handle(new GetUsersQuery(1, 20), CancellationToken.None);

        result.Count.ShouldBe(2);
        result[0].FirstName.ShouldBe("Alice");
        result[1].FirstName.ShouldBe("Bob");
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        _repositoryMock.Setup(r => r.GetPagedAsync(1, 20, It.IsAny<CancellationToken>())).ReturnsAsync([]);

        var result = await _handler.Handle(new GetUsersQuery(1, 20), CancellationToken.None);

        result.ShouldBeEmpty();
    }
}
