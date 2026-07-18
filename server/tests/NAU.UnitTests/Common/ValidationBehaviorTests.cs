using FluentValidation;
using MediatR;
using NAU.Application.Common.Behaviors;

namespace NAU.UnitTests.Common;

public class ValidationBehaviorTests
{
    private sealed record TestCommand(string Email) : IRequest<bool>;

    private sealed class TestCommandValidator : AbstractValidator<TestCommand>
    {
        public TestCommandValidator() => RuleFor(x => x.Email).NotEmpty().EmailAddress();
    }

    [Fact]
    public async Task Invalid_request_throws_before_handler_runs()
    {
        var behavior = new ValidationBehavior<TestCommand, bool>([new TestCommandValidator()]);
        var handlerRan = false;

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new TestCommand("not-an-email"), _ => { handlerRan = true; return Task.FromResult(true); },
                CancellationToken.None));

        Assert.False(handlerRan);
    }

    [Fact]
    public async Task Valid_request_reaches_handler()
    {
        var behavior = new ValidationBehavior<TestCommand, bool>([new TestCommandValidator()]);

        var result = await behavior.Handle(
            new TestCommand("alumni@example.com"),
            _ => Task.FromResult(true),
            CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task No_validators_registered_passes_through()
    {
        var behavior = new ValidationBehavior<TestCommand, bool>([]);

        var result = await behavior.Handle(new TestCommand(""), _ => Task.FromResult(true), CancellationToken.None);

        Assert.True(result);
    }
}
