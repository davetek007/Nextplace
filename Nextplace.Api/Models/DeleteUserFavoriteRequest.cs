using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class DeleteUserFavoriteRequest(string nextplaceId)
{
  [Required]
  public string NextplaceId { get; } = nextplaceId;
}