using Jex.Application.Common.Interfaces;
using Jex.Application.Common.Messaging;
using Jex.Domain.Entities;
using Jex.Domain.Events;

namespace Jex.IntegrationTests.Persistence;

/// <summary>
/// The unit of work really spans every repository in a command, nested commands and domain event handlers.
/// Test-only commands and handlers are registered through <see cref="CustomWebApplicationFactory.WithServices"/>.
/// </summary>
public sealed class TransactionRollbackTests : IDisposable
{
    private readonly CustomWebApplicationFactory _factory = new CustomWebApplicationFactory()
        .WithServices(services => services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(TransactionRollbackTests).Assembly)));

    public sealed record WriteTwoTablesThenFailCommand(string Suffix) : ICommand;

    public sealed class WriteTwoTablesThenFailHandler(IUserRepository users, ITodoListRepository lists) : IRequestHandler<WriteTwoTablesThenFailCommand>
    {
        public async Task Handle(WriteTwoTablesThenFailCommand request, CancellationToken cancellationToken)
        {
            await users.AddAsync(NewUser($"rollback-{request.Suffix}@example.com"), cancellationToken);
            await lists.AddAsync(new TodoList { Title = $"rollback-{request.Suffix}" }, cancellationToken);
            throw new InvalidOperationException("boom after two writes");
        }
    }

    public sealed record OuterCommand(string Suffix, bool FailAfterInner) : ICommand;

    public sealed record InnerCommand(string Suffix) : ICommand<long>;

    public sealed class OuterHandler(ITodoListRepository lists, ISender sender) : IRequestHandler<OuterCommand>
    {
        public async Task Handle(OuterCommand request, CancellationToken cancellationToken)
        {
            await lists.AddAsync(new TodoList { Title = $"outer-{request.Suffix}" }, cancellationToken);
            await sender.Send(new InnerCommand(request.Suffix), cancellationToken);

            if (request.FailAfterInner)
            {
                throw new InvalidOperationException("outer failed after inner succeeded");
            }
        }
    }

    public sealed class InnerHandler(ITodoListRepository lists, ITodoItemRepository items) : IRequestHandler<InnerCommand, long>
    {
        public async Task<long> Handle(InnerCommand request, CancellationToken cancellationToken)
        {
            var list = await lists.AddAsync(new TodoList { Title = $"inner-{request.Suffix}" }, cancellationToken);
            var item = new TodoItem { ListId = list.Id, Title = $"item-{request.Suffix}" };
            item.AddDomainEvent(new TodoItemCreatedEvent(item));
            await items.AddAsync(item, cancellationToken);
            return item.Id;
        }
    }

    /// <summary>Test-only event handler: writes a third row, and can be told to fail afterwards.</summary>
    public sealed class WriteThenMaybeFailOnCreated(ITodoListRepository lists) : INotificationHandler<TodoItemCreatedEvent>
    {
        private static volatile bool s_failAfterWrite;
        private static int s_invocations;

        public static bool FailAfterWrite { get => s_failAfterWrite; set => s_failAfterWrite = value; }

        public static int Invocations => s_invocations;

        public async Task Handle(TodoItemCreatedEvent notification, CancellationToken cancellationToken)
        {
            if (!notification.Item.Title.StartsWith("item-", StringComparison.Ordinal))
            {
                return;
            }

            Interlocked.Increment(ref s_invocations);
            await lists.AddAsync(new TodoList { Title = $"event-{notification.Item.Title}" }, cancellationToken);

            if (s_failAfterWrite)
            {
                throw new InvalidOperationException("event handler failed after writing");
            }
        }
    }

    [Fact]
    public async Task HandlerThrows_AfterWritingTwoTables_NothingIsPersisted()
    {
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await Should.ThrowAsync<InvalidOperationException>(() => sender.Send(new WriteTwoTablesThenFailCommand("a")));

        (await scope.ServiceProvider.GetRequiredService<IUserRepository>().ExistsAsync(u => u.Email == "rollback-a@example.com")).ShouldBeFalse();
        (await scope.ServiceProvider.GetRequiredService<ITodoListRepository>().ExistsAsync(l => l.Title == "rollback-a")).ShouldBeFalse();
    }

    [Fact]
    public async Task OuterFails_AfterNestedSucceeds_EverythingRollsBackAndEventsNeverRun()
    {
        WriteThenMaybeFailOnCreated.FailAfterWrite = false;
        var before = WriteThenMaybeFailOnCreated.Invocations;
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await Should.ThrowAsync<InvalidOperationException>(() => sender.Send(new OuterCommand("b", FailAfterInner: true)));

        var lists = scope.ServiceProvider.GetRequiredService<ITodoListRepository>();
        (await lists.ExistsAsync(l => l.Title == "outer-b")).ShouldBeFalse();
        (await lists.ExistsAsync(l => l.Title == "inner-b")).ShouldBeFalse();
        (await scope.ServiceProvider.GetRequiredService<ITodoItemRepository>().ExistsAsync(i => i.Title == "item-b")).ShouldBeFalse();
        WriteThenMaybeFailOnCreated.Invocations.ShouldBe(before, "events are dispatched only after the owner handler succeeds");
    }

    [Fact]
    public async Task EventHandlerFails_AfterWriting_EverythingRollsBack()
    {
        WriteThenMaybeFailOnCreated.FailAfterWrite = true;
        try
        {
            using var scope = _factory.Services.CreateScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            await Should.ThrowAsync<InvalidOperationException>(() => sender.Send(new OuterCommand("c", FailAfterInner: false)));

            var lists = scope.ServiceProvider.GetRequiredService<ITodoListRepository>();
            (await lists.ExistsAsync(l => l.Title == "outer-c")).ShouldBeFalse();
            (await lists.ExistsAsync(l => l.Title == "inner-c")).ShouldBeFalse();
            (await lists.ExistsAsync(l => l.Title == "event-item-c")).ShouldBeFalse("the event handler's write rolls back too");
        }
        finally
        {
            WriteThenMaybeFailOnCreated.FailAfterWrite = false;
        }
    }

    [Fact]
    public async Task NestedCommand_Succeeds_EventHandlerWriteIsCommitted()
    {
        WriteThenMaybeFailOnCreated.FailAfterWrite = false;
        using var scope = _factory.Services.CreateScope();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        await sender.Send(new OuterCommand("d", FailAfterInner: false));

        var lists = scope.ServiceProvider.GetRequiredService<ITodoListRepository>();
        (await lists.ExistsAsync(l => l.Title == "outer-d")).ShouldBeTrue();
        (await lists.ExistsAsync(l => l.Title == "inner-d")).ShouldBeTrue();
        (await lists.ExistsAsync(l => l.Title == "event-item-d")).ShouldBeTrue();
    }

    private static User NewUser(string email) => new()
    {
        UserName = email,
        Email = email,
        NormalizedUserName = email.ToUpperInvariant(),
        NormalizedEmail = email.ToUpperInvariant(),
        FirstName = "Roll",
        LastName = "Back",
    };

    public void Dispose() => _factory.Dispose();
}
