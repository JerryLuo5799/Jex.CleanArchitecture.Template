using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Jex.Domain.Common;

namespace Jex.Domain.Entities;

/// <summary>
/// Sample aggregate root. Soft-deletable: the title is unique among active lists only
/// (unique index on Title + DeletedToken, see <see cref="ISoftDelete"/>).
/// </summary>
[Table("todo_lists")]
public class TodoList : BaseAuditableEntity, ISoftDelete
{
    [MaxLength(200)]
    [Required]
    public string Title { get; set; } = string.Empty;

    /// <summary>Hex colour, e.g. <c>#FFFFFF</c>.</summary>
    [MaxLength(7)]
    public string Colour { get; set; } = "#FFFFFF";

    /// <summary>Child items (navigation configured in Infrastructure); loaded with <c>ITodoListRepository.GetWithItemsAsync</c>.</summary>
    public IList<TodoItem> Items { get; set; } = [];

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }

    public long? DeletedBy { get; set; }

    public long DeletedToken { get; set; }
}
