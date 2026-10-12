using Jex.Application.Features.Auth.Commands.Login;

namespace Jex.UnitTests.Features.Auth;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        _validator.TestValidate(new LoginCommand("user@example.com", "secret")).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("", "secret", nameof(LoginCommand.Email))]
    [InlineData("not-an-email", "secret", nameof(LoginCommand.Email))]
    [InlineData("user@example.com", "", nameof(LoginCommand.Password))]
    public void Validate_InvalidField_HasErrorForThatField(string email, string password, string member)
    {
        var result = _validator.TestValidate(new LoginCommand(email, password));

        result.Errors.ShouldContain(e => e.PropertyName == member);
    }
}
