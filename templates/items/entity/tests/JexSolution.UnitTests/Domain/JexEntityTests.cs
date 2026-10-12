using JexSolution.Application.Features.JexPlural;
using JexSolution.Domain.Entities;

namespace JexSolution.UnitTests.Domain;

public class JexEntityTests
{
    [Fact]
    public void Dto_From_CopiesFields()
    {
        var entity = new JexEntity { Id = 1, Name = "sample" };

        var dto = JexEntityDto.From(entity);

        dto.Id.ShouldBe(1);
        dto.Name.ShouldBe("sample");
        entity.DomainEvents.ShouldBeEmpty();
    }
}
