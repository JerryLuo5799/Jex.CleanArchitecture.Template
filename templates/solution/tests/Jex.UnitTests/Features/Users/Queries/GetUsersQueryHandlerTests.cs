using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Models;
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
    public async Task Handle_ReturnsPagedUserDtosWithTotal()
    {
        var users = new List<User>
        {
            new() { Id = 1, FirstName = "Alice", LastName = "A", Email = "alice@example.com" },
            new() { Id = 2, FirstName = "Bob", LastName = "B", Email = "bob@example.com" },
        };
        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedData<User>(users, 42, 1, 20));

        var result = await _handler.Handle(new GetUsersQuery(1, 20), CancellationToken.None);

        result.Items.Count.ShouldBe(2);
        result.TotalCount.ShouldBe(42);
        result.Items[0].FirstName.ShouldBe("Alice");
        result.Items[1].FirstName.ShouldBe("Bob");
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyPage()
    {
        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 20, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PagedData<User>([], 0, 1, 20));

        var result = await _handler.Handle(new GetUsersQuery(1, 20), CancellationToken.None);

        result.Items.ShouldBeEmpty();
        result.TotalCount.ShouldBe(0);
    }
}
