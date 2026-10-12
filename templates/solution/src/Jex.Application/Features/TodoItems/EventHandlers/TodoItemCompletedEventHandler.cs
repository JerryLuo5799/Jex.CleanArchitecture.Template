using Jex.Domain.Events;

namespace Jex.Application.Features.TodoItems.EventHandlers;

public sealed class TodoItemCompletedEventHandler(ILogger<TodoItemCompletedEventHandler> logger)
    : INotificationHandler<TodoItemCompletedEvent>
{
    public Task Handle(TodoItemCompletedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("Domain event {Event}: item {ItemId} completed",
            nameof(TodoItemCompletedEvent), notification.Item.Id);

        return Task.CompletedTask;
    }
}
