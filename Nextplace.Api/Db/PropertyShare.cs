using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Api.Db;

public sealed class PropertyShare : EntityTableData
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public new long Id { get; init; }

  public required long PropertyId { get; init; }

  [MaxLength(450)]
  public required string ShareRef { get; set; }

  [MaxLength(450)]
  public required string SenderEmailAddress { get; set; }

  [MaxLength(450)]
  public required string ReceiverEmailAddress { get; set; }

  [MaxLength(450)]
  public required string Message { get; set; }

  [MaxLength(450)]
  public required int ViewCount { get; set; }

  public required DateTime CreateDate { get; set; }

  public required DateTime LastUpdateDate { get; set; }

  public required bool Active { get; set; }

  public Property Property { get; init; } = null!;
}