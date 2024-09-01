using Microsoft.AspNetCore.Datasync.EFCore;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace Nextplace.Api.Db;

public sealed class ApiLog : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    [MaxLength(450)]
    public required string ApiName { get; init; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public required string LogEntry { get; init; }

    [MaxLength(450)]
    public required string EntryType { get; init; }

    public required DateTime TimeStamp { get; init; }

    [MaxLength(450)]
    public required string ExecutionInstanceId { get; init; }
}