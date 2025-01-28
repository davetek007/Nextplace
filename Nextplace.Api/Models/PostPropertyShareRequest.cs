using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PostPropertyShareRequest(
  string senderEmailAddress,
  string receiverEmailAddress,
  string message,
  string nextplaceId)
{
  [Required]
  public string SenderEmailAddress { get; } = senderEmailAddress;

  [Required]
  public string ReceiverEmailAddress { get; } = receiverEmailAddress;

  [Required]
  public string Message { get; } = message;

  [Required]
  public string NextplaceId { get; } = nextplaceId;
}