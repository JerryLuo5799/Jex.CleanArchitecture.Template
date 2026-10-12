using Jex.Application.Common.Models;
using Jex.Domain.Entities;

namespace Jex.Application.Common.Interfaces;

/// <summary>Repository for <see cref="TodoItem"/>.</summary>
public interface ITodoItemRepository : IRepository<TodoItem>
{
    Task<PagedData<TodoItem>> GetByListAsync(long listId, int page, int pageSize, CancellationToken cancellationToken = default);
}
