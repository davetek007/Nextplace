using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Api.Db;

public sealed class PropertyEstimate : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    public required long PropertyId { get; init; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public required DateTime DateEstimated { get; set; }

    public required double Estimate { get; set; }

    public required DateTime CreateDate { get; set; }

    public required DateTime LastUpdateDate { get; set; }

    public required bool Active { get; set; }

    public Property Property { get; init; } = null!;
}