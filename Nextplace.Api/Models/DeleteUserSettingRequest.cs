using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class DeleteUserSettingRequest(string settingName)
{
  [Required]
  public string SettingName { get; } = settingName;
}