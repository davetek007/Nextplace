using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class ResetPasswordRequest(string emailAddress, string validationKey, string password)
{
  [Required]
  public string EmailAddress { get; } = emailAddress;

  [Required]
  public string ValidationKey { get; } = validationKey;

  [Required]
  public string Password { get; } = password;
}