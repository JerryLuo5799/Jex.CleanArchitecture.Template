using Jex.Application.Common.Interfaces;
using Jex.Application.Features.Users.Commands.CreateUser;

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
        var command = new CreateUserCommand("John", "Doe", "john@example.com", "Password1!");

        _repositoryMock
            .Setup(r => r.IsEmailUniqueAsync(command.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _identityServiceMock
            .Setup(s => s.CreateUserAsync(command.FirstName, command.LastName, command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ReturnsAsync(42L);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.ShouldBe(42L);
        _identityServiceMock.Verify(
            s => s.CreateUserAsync(command.FirstName, command.LastName, command.Email, command.Password, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateEmail_ThrowsValidationException()
    {
        var command = new CreateUserCommand("Jane", "Doe", "duplicate@example.com", "Password1!");

        _repositoryMock
            .Setup(r => r.IsEmailUniqueAsync(command.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var exception = await Should.ThrowAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Errors.ShouldContainKey(nameof(command.Email));
        _identityServiceMock.Verify(
            s => s.CreateUserAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_IdentityFailure_PropagatesValidationException()
    {
        var command = new CreateUserCommand("Alice", "Smith", "alice@example.com", "weak");

        _repositoryMock
            .Setup(r => r.IsEmailUniqueAsync(command.Email, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _identityServiceMock
            .Setup(s => s.CreateUserAsync(command.FirstName, command.LastName, command.Email, command.Password, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException("password", "Password is too weak."));

        var exception = await Should.ThrowAsync<ValidationException>(
            () => _handler.Handle(command, CancellationToken.None));

        exception.Errors.ShouldContainKey("password");
    }
}
