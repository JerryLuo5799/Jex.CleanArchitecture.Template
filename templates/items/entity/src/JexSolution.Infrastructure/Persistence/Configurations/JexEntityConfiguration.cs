using JexSolution.Domain.Entities;

namespace JexSolution.Infrastructure.Persistence.Configurations;

/// <summary>Discovered automatically: registers <see cref="JexEntity"/> for schema sync and migrations.</summary>
public sealed class JexEntityConfiguration : EntityConfiguration<JexEntity>
{
    protected override void Configure(TableFluent<JexEntity> table)
    {
        table.Property(e => e.Id).IsPrimary(true).IsIdentity(true);
        // TODO: indexes, e.g. table.Index("ix_TPLTABLE_name", nameof(JexEntity.Name), false);
    }
}
