using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class ValidateUserRequest(string emailAddress, string validationKey)
{
  [Required]
  public string EmailAddress { get; } = emailAddress;

  [Required]
  public string ValidationKey { get; } = validationKey;
}