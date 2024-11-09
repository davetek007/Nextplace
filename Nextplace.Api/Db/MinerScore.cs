using Microsoft.AspNetCore.Datasync.EFCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Db;

public sealed class MinerScore : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    public required long MinerId { get; init; }

    public long? ValidatorId { get; set; }

    public required double Score { get; set; }

    public required int NumPredictions { get; set; }

    public required int TotalPredictions { get; set; }

    public required DateTime ScoreGenerationDate { get; set; }

    public required DateTime CreateDate { get; set; }

    public required DateTime LastUpdateDate { get; set; }

    public required bool Active { get; set; }

    public Miner Miner { get; init; } = null!;

    public Validator? Validator { get; init; }
}