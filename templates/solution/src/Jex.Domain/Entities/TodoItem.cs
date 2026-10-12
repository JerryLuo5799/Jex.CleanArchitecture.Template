using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jex.Domain.Common;
using Jex.Domain.Enums;
using Jex.Domain.Events;

namespace Jex.Domain.Entities;

/// <summary>Sample child entity of <see cref="TodoList"/>. Completing an item raises <see cref="TodoItemCompletedEvent"/>.</summary>
[Table("todo_items")]
public class TodoItem : BaseAuditableEntity
{
    private bool _done;

    public long ListId { get; set; }

    [MaxLength(200)]
    [Required]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Note { get; set; }

    public PriorityLevel Priority { get; set; }

    public DateTime? Reminder { get; set; }

    public bool Done
    {
        get => _done;
        set
        {
            if (value && !_done)
            {
                AddDomainEvent(new TodoItemCompletedEvent(this));
            }

            _done = value;
        }
    }
}
