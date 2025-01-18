using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class AddUserFavoriteRequest(string nextplaceId)
{
  [Required]
  public string NextplaceId { get; } = nextplaceId;
}