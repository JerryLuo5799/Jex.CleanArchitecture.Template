using JexSolution.Domain.Entities;

namespace JexSolution.Application.Features.JexPlural;

/// <summary>Public shape of <see cref="JexEntity"/>. Mapping is hand-written next to the DTO (ADR-0009).</summary>
public sealed record JexEntityDto(long Id, string Name, DateTime CreatedAt)
{
    public static JexEntityDto From(JexEntity entity) => new(entity.Id, entity.Name, entity.CreatedAt);
}
