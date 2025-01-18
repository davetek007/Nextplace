using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class AddUserSettingRequest(string settingName, string settingValue)
{
  [Required]
  public string SettingName { get; } = settingName;
  [Required]
  public string SettingValue { get; } = settingValue;
}