using FluentValidation.TestHelper;
using Jex.Application.Features.Users.Commands.CreateUser;

namespace Jex.UnitTests.Features.Users.Commands;

public class CreateUserCommandValidatorTests
{
    private readonly CreateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new CreateUserCommand("John", "Doe", "john@example.com", "Password1!"));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "Doe", "john@example.com", "Password1!", nameof(CreateUserCommand.FirstName))]
    [InlineData("John", "", "john@example.com", "Password1!", nameof(CreateUserCommand.LastName))]
    [InlineData("John", "Doe", "not-an-email", "Password1!", nameof(CreateUserCommand.Email))]
    [InlineData("John", "Doe", "john@example.com", "short", nameof(CreateUserCommand.Password))]
    public void Validate_InvalidField_HasErrorForThatField(
        string firstName, string lastName, string email, string password, string expectedMember)
    {
        var result = _validator.TestValidate(new CreateUserCommand(firstName, lastName, email, password));

        result.Errors.ShouldContain(e => e.PropertyName == expectedMember);
    }
}
