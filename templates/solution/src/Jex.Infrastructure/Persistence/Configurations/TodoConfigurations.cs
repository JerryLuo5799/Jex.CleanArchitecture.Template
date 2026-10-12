using Jex.Domain.Entities;

namespace Jex.Infrastructure.Persistence.Configurations;

public sealed class TodoListConfiguration : EntityConfiguration<TodoList>
{
    protected override void Configure(TableFluent<TodoList> table)
    {
        table.Property(l => l.Id).IsPrimary(true).IsIdentity(true);
        // "Only active rows are unique": DeletedToken is 0 while active and the row's Id once soft-deleted.
        table.Index("uk_todo_lists_title", $"{nameof(TodoList.Title)}, {nameof(TodoList.DeletedToken)}", true);
        table.Navigate(l => l.Items, nameof(TodoItem.ListId));
    }
}

public sealed class TodoItemConfiguration : EntityConfiguration<TodoItem>
{
    protected override void Configure(TableFluent<TodoItem> table)
    {
        table.Property(i => i.Id).IsPrimary(true).IsIdentity(true);
        table.Index("ix_todo_items_list_id", nameof(TodoItem.ListId), false);
    }
}
