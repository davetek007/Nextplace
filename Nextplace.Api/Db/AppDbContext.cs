using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Nextplace.Api.Db;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Miner> Miner => Set<Miner>();

    public DbSet<Property> Property => Set<Property>();

    public DbSet<PropertyEstimateStats> PropertyEstimateStats => Set<PropertyEstimateStats>();

    public DbSet<ApiLog> ApiLog => Set<ApiLog>();

    public DbSet<PropertyPrediction> PropertyPrediction => Set<PropertyPrediction>();

    public DbSet<Validator> Validator => Set<Validator>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PropertyEstimateStats>().HasOne(tgp => tgp.Property).WithMany(m => m.EstimateStats)
            .HasForeignKey(tgp => tgp.PropertyId); base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<PropertyPrediction>().HasOne(tgp => tgp.Miner).WithMany(m => m.Predictions)
            .HasForeignKey(tgp => tgp.MinerId);

        modelBuilder.Entity<PropertyPrediction>().HasOne(tgp => tgp.Validator).WithMany(m => m.Predictions)
            .HasForeignKey(tgp => tgp.ValidatorId);

        modelBuilder.Entity<PropertyPrediction>().HasOne(tgp => tgp.Property).WithMany(m => m.Predictions)
            .HasForeignKey(tgp => tgp.PropertyId); base.OnModelCreating(modelBuilder);
      
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            modelBuilder.Entity(entityType.ClrType)
                .Ignore("Deleted")
                .Ignore("UpdatedAt")
                .Ignore("Version");
        }
    }

    public async Task SaveLogEntry(string apiName, string logEntry, string entryType, string executionInstanceId)
    {
        ApiLog.Add(new ApiLog
        {
            ApiName = apiName,
            LogEntry = logEntry,
            EntryType = entryType,
            TimeStamp = DateTime.UtcNow,
            ExecutionInstanceId = executionInstanceId
        });

        await SaveChangesAsync();
    }

    public async Task SaveLogEntry(string apiName, Exception ex, string executionInstanceId)
    {
        ApiLog.Add(new ApiLog
        {
            ApiName = apiName,
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
        while (true)
        {
            var indent = new string(' ', level * 2);
            sb.AppendLine($"{indent}Exception: {ex.GetType().FullName}");
            sb.AppendLine($"{indent}Message: {ex.Message}");
            sb.AppendLine($"{indent}StackTrace: {ex.StackTrace}");

            if (ex.InnerException != null)
            {
                sb.AppendLine($"{indent}Inner Exception:");
                ex = ex.InnerException;
                level += 1;
                continue;
            }

            break;
        }
    }
}