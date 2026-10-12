using Jex.Domain.Entities;

namespace Jex.Application.Common.Interfaces;

/// <summary>Repository for the <see cref="TodoList"/> aggregate. Implemented in Infrastructure and registered by convention.</summary>
public interface ITodoListRepository : IRepository<TodoList>
{
    /// <summary>Loads a list with its items.</summary>
    Task<TodoList?> GetWithItemsAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>All active lists with their items, ordered by title.</summary>
    Task<List<TodoList>> GetAllWithItemsAsync(CancellationToken cancellationToken = default);

    Task<bool> IsTitleUniqueAsync(string title, long? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>Physically removes every soft-deleted list (and its items). Returns the number of lists purged.</summary>
    Task<int> PurgeDeletedAsync(CancellationToken cancellationToken = default);
}
