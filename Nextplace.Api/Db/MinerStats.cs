using Microsoft.AspNetCore.Datasync.EFCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Db;

public sealed class MinerStats : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    public required long MinerId { get; init; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public required string StatType { get; set; }

    public required int Ranking { get; set; }

    public required int NumberOfPredictions { get; set; }

    public required int CorrectPredictions { get; set; }

    public Miner Miner { get; init; } = null!;
}