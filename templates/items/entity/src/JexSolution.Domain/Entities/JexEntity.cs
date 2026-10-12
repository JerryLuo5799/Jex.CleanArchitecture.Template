using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using JexSolution.Domain.Common;

namespace JexSolution.Domain.Entities;

/// <summary>TODO: describe the JexEntity aggregate. Implement ISoftDelete if it should be soft-deleted.</summary>
[Table("TPLTABLE")]
public class JexEntity : BaseAuditableEntity
{
    [MaxLength(200)]
    [Required]
    public string Name { get; set; } = string.Empty;
}
