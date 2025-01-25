using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Api.Db;

public sealed class Market : EntityTableData
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public new int Id { get; init; }

  [MaxLength(450)]
  public required string Name { get; init; }

  [MaxLength(450)] public string Country { get; set; }

  [MaxLength(450)]
  public required string ExternalId { get; init; }

  public required DateTime CreateDate { get; set; }

  public required DateTime LastUpdateDate { get; set; }

  public required bool Active { get; set; }
}