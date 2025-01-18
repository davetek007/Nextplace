using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class DeleteUserRequest(string emailAddress, string password)
{
  [Required]
  public string EmailAddress { get; } = emailAddress;

  [Required]
  public string Password { get; } = password;
}