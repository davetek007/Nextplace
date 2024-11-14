using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Models;

public class Validator(string hotKey, string coldKey)
{
    [Required]
    public string HotKey { get; } = hotKey;

    [Required]
    public string ColdKey { get; } = coldKey;

    public string? Version { get; set; }
}