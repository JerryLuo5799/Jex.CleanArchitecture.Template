using Jex.Application.Common.Behaviours;

namespace Jex.UnitTests.Common.Behaviours;

public class ValidationBehaviourTests
{
    private sealed record Ping(string Name) : IRequest<string>;

    private sealed class PingValidator : AbstractValidator<Ping>
    {
        public PingValidator() => RuleFor(x => x.Name).NotEmpty().MaximumLength(5);
    }

    private static Task<string> Next(CancellationToken _) => Task.FromResult("pong");

    [Fact]
    public async Task Handle_NoValidators_CallsNext()
    {
        var behaviour = new ValidationBehaviour<Ping, string>([]);

        var result = await behaviour.Handle(new Ping(""), Next, CancellationToken.None);

        result.ShouldBe("pong");
    }

    [Fact]
    public async Task Handle_ValidRequest_CallsNext()
    {
        var behaviour = new ValidationBehaviour<Ping, string>([new PingValidator()]);

        var result = await behaviour.Handle(new Ping("ok"), Next, CancellationToken.None);

        result.ShouldBe("pong");
    }

    [Fact]
    public async Task Handle_InvalidRequest_ThrowsValidationExceptionWithErrorsByMember()
    {
        var behaviour = new ValidationBehaviour<Ping, string>([new PingValidator()]);

        var exception = await Should.ThrowAsync<ValidationException>(
            () => behaviour.Handle(new Ping(""), Next, CancellationToken.None));

        exception.Code.ShouldBe(4000);
        exception.Errors.ShouldContainKey(nameof(Ping.Name));
    }
}
