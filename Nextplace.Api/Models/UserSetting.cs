namespace Nextplace.Api.Models;

public class UserSetting(string settingName, string settingValue)
{
  public string SettingName { get; } = settingName;
  
  public string SettingValue { get; } = settingValue;
}