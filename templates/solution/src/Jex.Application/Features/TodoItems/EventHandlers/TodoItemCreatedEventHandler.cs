using Jex.Domain.Events;

namespace Jex.Application.Features.TodoItems.EventHandlers;

/// <summary>
/// Runs inside the command's transaction, before commit. Only database work belongs here; external
/// side effects (email, HTTP) need an outbox (ADR-0005).
/// </summary>
public sealed class TodoItemCreatedEventHandler(ILogger<TodoItemCreatedEventHandler> logger)
    : INotificationHandler<TodoItemCreatedEvent>
{
    public Task Handle(TodoItemCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Domain event {Event}: item {ItemId} created in list {ListId}",
            nameof(TodoItemCreatedEvent), notification.Item.Id, notification.Item.ListId);

        return Task.CompletedTask;
    }
}
