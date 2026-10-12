using Jex.Domain.Entities;
using Jex.Domain.Enums;

namespace Jex.Application.Features.TodoLists.Queries.GetTodos;

public sealed record TodosVm(IReadOnlyList<LookupDto> PriorityLevels, IReadOnlyList<TodoListDto> Lists);

public sealed record LookupDto(int Id, string Title);

public sealed record TodoListDto(long Id, string Title, string Colour, IReadOnlyList<TodoItemDto> Items)
{
    public static TodoListDto From(TodoList list) =>
        new(list.Id, list.Title, list.Colour, list.Items.Select(TodoItemDto.From).ToList());
}

public sealed record TodoItemDto(long Id, long ListId, string Title, string? Note, PriorityLevel Priority, DateTime? Reminder, bool Done)
{
    public static TodoItemDto From(TodoItem item) =>
        new(item.Id, item.ListId, item.Title, item.Note, item.Priority, item.Reminder, item.Done);
}
