using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Datasync.EFCore;

namespace Nextplace.Functions.Db;

public sealed class FunctionLog : EntityTableData
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public new long Id { get; init; }

    [MaxLength(450)]
    public required string FunctionName { get; init; }

    // ReSharper disable once EntityFramework.ModelValidation.UnlimitedStringLength
    public required string LogEntry { get; init; }

    [MaxLength(450)]
    public required string EntryType { get; init; }

    public required DateTime TimeStamp { get; init; }

    [MaxLength(450)]
    public required string ExecutionInstanceId { get; init; }
}