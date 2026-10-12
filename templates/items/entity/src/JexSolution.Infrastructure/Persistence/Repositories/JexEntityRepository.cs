using JexSolution.Application.Common.Events;
using JexSolution.Application.Common.Interfaces;
using JexSolution.Domain.Entities;

namespace JexSolution.Infrastructure.Persistence.Repositories;

/// <summary>Registered automatically against <see cref="IJexEntityRepository"/>. Use <c>Query</c> for tracked reads.</summary>
public sealed class JexEntityRepository(IFreeSql freeSql, UnitOfWorkManager unitOfWorkManager, AuditStamper audit, PendingDomainEvents events)
    : Repository<JexEntity>(freeSql, unitOfWorkManager, audit, events), IJexEntityRepository;
