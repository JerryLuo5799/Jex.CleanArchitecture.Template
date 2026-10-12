using Jex.Domain.Entities;
using Jex.Domain.Events;

namespace Jex.UnitTests.Domain;

public class TodoItemTests
{
    [Fact]
    public void Done_SetToTrue_RaisesCompletedEventOnce()
    {
        var item = new TodoItem { Title = "Ship it" };

        item.Done = true;
        item.Done = true;

        item.DomainEvents.ShouldHaveSingleItem().ShouldBeOfType<TodoItemCompletedEvent>();
    }

    [Fact]
    public void Done_SetToFalse_RaisesNothing()
    {
        var item = new TodoItem { Title = "Not yet" };

        item.Done = false;

        item.DomainEvents.ShouldBeEmpty();
    }

    [Fact]
    public void ClearDomainEvents_EmptiesTheQueue()
    {
        var item = new TodoItem { Title = "Clear" };
        item.Done = true;

        item.ClearDomainEvents();

        item.DomainEvents.ShouldBeEmpty();
    }
}
