using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Api.Db;

public sealed class User : EntityTableData
{
  [Key]
  [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
  public new long Id { get; init; }

  [MaxLength(450)]
  public required string EmailAddress { get; init; }

  [MaxLength(450)]
  public required string Password { get; set; }

  [MaxLength(450)]
  public required string Salt { get; set; }

  [MaxLength(450)]
  public required string UserType { get; set; }

  [MaxLength(450)]
  public string? ValidationKey { get; set; }

  [MaxLength(450)]
  public string? SessionToken { get; set; }

  [MaxLength(450)]
  public required string Status { get; set; }

  public required DateTime CreateDate { get; set; }

  public required DateTime LastUpdateDate { get; set; }

  public required bool Active { get; set; }

  // ReSharper disable once CollectionNeverUpdated.Global
  public ICollection<UserFavorite>? UserFavorites { get; init; }

  // ReSharper disable once CollectionNeverUpdated.Global
  public ICollection<UserSetting>? UserSettings { get; init; }
}