﻿using System.Text;
using Microsoft.EntityFrameworkCore;

namespace Nextplace.Functions.Db;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<FunctionLog> FunctionLog => Set<FunctionLog>();

    public DbSet<Market> Market => Set<Market>();

    public DbSet<Property> Property => Set<Property>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            modelBuilder.Entity(entityType.ClrType)
                .Ignore("Deleted")
                .Ignore("UpdatedAt")
                .Ignore("Version");
        }
    }

    public async Task SaveLogEntry(string functionName, string logEntry, string entryType, string executionInstanceId)
    {
        FunctionLog.Add(new FunctionLog
        {
            FunctionName = functionName,
            LogEntry = logEntry,
            EntryType = entryType,
            TimeStamp = DateTime.UtcNow, 
            ExecutionInstanceId = executionInstanceId
        });

        await SaveChangesAsync();
    }

    public async Task SaveLogEntry(string functionName, Exception ex, string executionInstanceId)
    {
        FunctionLog.Add(new FunctionLog
        {
            FunctionName = functionName,
            LogEntry = ExceptionToString(ex),
            EntryType = "Error",
            TimeStamp = DateTime.UtcNow,
            ExecutionInstanceId = executionInstanceId
        });

        await SaveChangesAsync();
    }

    public static string ExceptionToString(Exception ex)
    {
        var sb = new StringBuilder();
        FormatException(sb, ex, 0);

        return sb.ToString();
    }

    private static void FormatException(StringBuilder sb, Exception ex, int level)
    {
        var indent = new string(' ', level * 2);
        sb.AppendLine($"{indent}Exception: {ex.GetType().FullName}");
        sb.AppendLine($"{indent}Message: {ex.Message}");
        sb.AppendLine($"{indent}StackTrace: {ex.StackTrace}");

        if (ex.InnerException != null)
        {
            sb.AppendLine($"{indent}Inner Exception:");
            FormatException(sb, ex.InnerException, level + 1);
        }
    }
}