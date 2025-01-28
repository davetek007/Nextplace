using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PostPropertyShareViewRequest(
  string shareRef)
{
  [Required]
  public string ShareRef { get; } = shareRef;
}