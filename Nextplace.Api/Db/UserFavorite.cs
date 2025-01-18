using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Api.Db;

public sealed class UserFavorite : EntityTableData
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public new long Id { get; init; }

  public long UserId { get; init; }

  [MaxLength(450)]
  public required string NextplaceId { get; set; }

  public required DateTime CreateDate { get; set; }

  public required DateTime LastUpdateDate { get; set; }

  public required bool Active { get; set; }

  public User User { get; set; } = null!;
}