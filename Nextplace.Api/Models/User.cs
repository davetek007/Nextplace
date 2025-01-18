using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class User(long id, string emailAddress, List<UserFavorite> userFavorites, List<UserSetting> userSettings)
{
  [Required] public long Id { get; } = id;

  [Required] public string EmailAddress { get; } = emailAddress;

  [Required] public List<UserFavorite> UserFavorites { get; } = userFavorites;

  [Required] public List<UserSetting> UserSettings { get; } = userSettings;
}