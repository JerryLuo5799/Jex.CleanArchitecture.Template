using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Features.Users.Queries.GetUserById;
using Jex.Domain.Entities;
using Jex.Domain.Enums;
using Moq;

namespace Jex.UnitTests.Features.Users.Queries;

public class GetUserByIdQueryHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock = new();
    private readonly GetUserByIdQueryHandler _handler;

    public GetUserByIdQueryHandlerTests()
    {
        _handler = new GetUserByIdQueryHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingUser_ReturnsUserDto()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            PasswordHash = "$2a$11$hash",
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, default)).ReturnsAsync(user);

        // Act
        var dto = await _handler.Handle(new GetUserByIdQuery(1), CancellationToken.None);

        // Assert
        Assert.NotNull(dto);
        Assert.Equal(1, dto.Id);
        Assert.Equal("Alice", dto.FirstName);
        Assert.Equal("Smith", dto.LastName);
        Assert.Equal("alice@example.com", dto.Email);
        Assert.Equal(UserStatus.Active, dto.Status);
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsNotFoundException()
    {
        // Arrange
        _repositoryMock.Setup(r => r.GetByIdAsync(99, default)).ReturnsAsync((User?)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => _handler.Handle(new GetUserByIdQuery(99), CancellationToken.None));
    }
}
