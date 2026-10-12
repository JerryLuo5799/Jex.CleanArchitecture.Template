using JexSolution.Domain.Entities;

namespace JexSolution.Application.Common.Interfaces;

/// <summary>Repository for <see cref="JexEntity"/>. Add entity-specific queries here; the implementation is registered by convention.</summary>
public interface IJexEntityRepository : IRepository<JexEntity>;
