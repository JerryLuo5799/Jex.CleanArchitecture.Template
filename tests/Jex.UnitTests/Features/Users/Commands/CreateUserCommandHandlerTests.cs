using Jex.Application.Common.Exceptions;
using Jex.Application.Common.Interfaces;
using Jex.Application.Features.Users.Commands.CreateUser;
using Jex.Domain.Entities;
using Jex.Domain.Enums;
using Moq;

namespace Jex.UnitTests.Features.Users.Commands;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock = new();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _handler = new CreateUserCommandHandler(_repositoryMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserAndReturnsId()
    {
        // Arrange
        var command = new CreateUserCommand("John", "Doe", "john@example.com", "Password1!");
        var createdUser = new User { Id = 42, FirstName = "John", LastName = "Doe", Email = "john@example.com", Status = UserStatus.Active };

        _repositoryMock
            .Setup(r => r.IsEmailUniqueAsync(command.Email, null, default))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), default))
            .ReturnsAsync(createdUser);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(42, result);
        _repositoryMock.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.FirstName == "John" &&
            u.LastName == "Doe" &&
            u.Email == "john@example.com" &&
            u.Status == UserStatus.Active &&
            !string.IsNullOrEmpty(u.PasswordHash)), default), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsValidationException()
    {
        // Arrange
        var command = new CreateUserCommand("Jane", "Doe", "duplicate@example.com", "Password1!");

        _repositoryMock
            .Setup(r => r.IsEmailUniqueAsync(command.Email, null, default))
            .ReturnsAsync(false);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey(nameof(command.Email)));
        _repositoryMock.Verify(r => r.AddAsync(It.IsAny<User>(), default), Times.Never);
    }

    [Fact]
    public async Task Handle_ValidCommand_HashesPasswordBeforeSaving()
    {
        // Arrange
        const string plainPassword = "MySecret123!";
        var command = new CreateUserCommand("Alice", "Smith", "alice@example.com", plainPassword);
        var createdUser = new User { Id = 1, Email = "alice@example.com" };

        _repositoryMock
            .Setup(r => r.IsEmailUniqueAsync(command.Email, null, default))
            .ReturnsAsync(true);
        _repositoryMock
            .Setup(r => r.AddAsync(It.IsAny<User>(), default))
            .ReturnsAsync(createdUser);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert – password must be stored as a BCrypt hash, not plaintext
        _repositoryMock.Verify(r => r.AddAsync(
            It.Is<User>(u => u.PasswordHash != plainPassword && u.PasswordHash.StartsWith("$2")),
            default), Times.Once);
    }
}
