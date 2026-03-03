using Jex.Application.Common;
using Jex.Application.Features.Users.Queries.GetUsers;
using Jex.Domain.Entities;
using Jex.Domain.Enums;
using Moq;

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
        // Arrange
        var users = new List<User>
        {
            new() { Id = 1, FirstName = "Alice", LastName = "A", Email = "alice@example.com", Status = UserStatus.Active },
            new() { Id = 2, FirstName = "Bob",   LastName = "B", Email = "bob@example.com",   Status = UserStatus.Inactive }
        };
        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 20, default))
            .ReturnsAsync(users);

        // Act
        var result = await _handler.Handle(new GetUsersQuery(1, 20), CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("Alice", result[0].FirstName);
        Assert.Equal("Bob",   result[1].FirstName);
    }

    [Fact]
    public async Task Handle_EmptyRepository_ReturnsEmptyList()
    {
        // Arrange
        _repositoryMock
            .Setup(r => r.GetPagedAsync(1, 20, default))
            .ReturnsAsync([]);

        // Act
        var result = await _handler.Handle(new GetUsersQuery(1, 20), CancellationToken.None);

        // Assert
        Assert.Empty(result);
    }
}
