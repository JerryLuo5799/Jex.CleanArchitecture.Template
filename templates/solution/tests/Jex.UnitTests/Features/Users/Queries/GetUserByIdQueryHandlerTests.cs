using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Features.Users.Queries.GetUserById;
using Jex.Domain.Entities;
using Jex.Domain.Enums;

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
        var user = new User
        {
            Id = 1,
            FirstName = "Alice",
            LastName = "Smith",
            Email = "alice@example.com",
            Status = UserStatus.Active,
            CreatedAt = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        };
        _repositoryMock.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);

        var dto = await _handler.Handle(new GetUserByIdQuery(1), CancellationToken.None);

        dto.Id.ShouldBe(1);
        dto.FirstName.ShouldBe("Alice");
        dto.LastName.ShouldBe("Smith");
        dto.Email.ShouldBe("alice@example.com");
        dto.Status.ShouldBe(UserStatus.Active);
        dto.FullName.ShouldBe("Alice Smith");
    }

    [Fact]
    public async Task Handle_NonExistentUser_ThrowsNotFoundException()
    {
        _repositoryMock.Setup(r => r.GetByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await Should.ThrowAsync<NotFoundException>(() => _handler.Handle(new GetUserByIdQuery(99), CancellationToken.None));
    }
}
