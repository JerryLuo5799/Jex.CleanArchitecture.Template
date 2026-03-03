using Jex.Application.Common.Exceptions;
using Jex.Application.Common;
using Jex.Application.Features.Users.Commands.CreateUser;
using Moq;

namespace Jex.UnitTests.Features.Users.Commands;

public class CreateUserCommandHandlerTests
{
    private readonly Mock<IUserRepository> _repositoryMock = new();
    private readonly Mock<IIdentityService> _identityServiceMock = new();
    private readonly CreateUserCommandHandler _handler;

    public CreateUserCommandHandlerTests()
    {
        _handler = new CreateUserCommandHandler(_repositoryMock.Object, _identityServiceMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_CreatesUserAndReturnsId()
    {
        // Arrange
        var command = new CreateUserCommand("John", "Doe", "john@example.com", "Password1!");

        _repositoryMock
            .Setup(r => r.IsEmailUniqueAsync(command.Email, null, default))
            .ReturnsAsync(true);
        _identityServiceMock
            .Setup(s => s.CreateUserAsync(command.FirstName, command.LastName, command.Email, command.Password, default))
            .ReturnsAsync((42L, true, Enumerable.Empty<string>()));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(42, result);
        _identityServiceMock.Verify(
            s => s.CreateUserAsync(command.FirstName, command.LastName, command.Email, command.Password, default),
            Times.Once);
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
        _identityServiceMock.Verify(
            s => s.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), default),
            Times.Never);
    }

    [Fact]
    public async Task Handle_IdentityFailure_ThrowsValidationException()
    {
        // Arrange
        var command = new CreateUserCommand("Alice", "Smith", "alice@example.com", "weak");

        _repositoryMock
            .Setup(r => r.IsEmailUniqueAsync(command.Email, null, default))
            .ReturnsAsync(true);
        _identityServiceMock
            .Setup(s => s.CreateUserAsync(command.FirstName, command.LastName, command.Email, command.Password, default))
            .ReturnsAsync((0L, false, new[] { "Password is too weak." }));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));

        Assert.True(exception.Errors.ContainsKey(nameof(command.Password)));
    }
}
