using Jex.Application.Features.Users.Commands.UpdateUser;
using Jex.Domain.Enums;

namespace Jex.UnitTests.Features.Users.Commands;

public class UpdateUserCommandValidatorTests
{
    private readonly UpdateUserCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_HasNoErrors()
    {
        var result = _validator.TestValidate(new UpdateUserCommand(1, "Jane", "Doe", "jane@example.com", UserStatus.Active));

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_InvalidIdEmailAndStatus_ReportsEachMember()
    {
        var result = _validator.TestValidate(new UpdateUserCommand(0, "Jane", "Doe", "bad", (UserStatus)99));

        result.ShouldHaveValidationErrorFor(x => x.Id);
        result.ShouldHaveValidationErrorFor(x => x.Email);
        result.ShouldHaveValidationErrorFor(x => x.Status);
    }
}
