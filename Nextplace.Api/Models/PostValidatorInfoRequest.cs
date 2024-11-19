using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class PostValidatorInfoRequest(string version)
{
    [Required]
    public string Version { get; } = version;
}