using Microsoft.EntityFrameworkCore;
using System.Text;

namespace Nextplace.Api.Db;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
  public DbSet<Miner> Miner => Set<Miner>();

  public DbSet<MinerDatedScore> MinerDatedScore => Set<MinerDatedScore>();

  public DbSet<Property> Property => Set<Property>();

  public DbSet<PropertyValuation> PropertyValuation => Set<PropertyValuation>();

  public DbSet<PropertyValuationPrediction> PropertyValuationPrediction => Set<PropertyValuationPrediction>();

  public DbSet<PropertyImage> PropertyImage => Set<PropertyImage>();

  public DbSet<PropertyEstimateStats> PropertyEstimateStats => Set<PropertyEstimateStats>();

  public DbSet<ApiLog> ApiLog => Set<ApiLog>();

  public DbSet<MinerScore> MinerScore => Set<MinerScore>();

  public DbSet<PropertyPrediction> PropertyPrediction => Set<PropertyPrediction>();

  public DbSet<Validator> Validator => Set<Validator>();

  public DbSet<User> User => Set<User>();

  public DbSet<UserSetting> UserSetting => Set<UserSetting>();

  public DbSet<UserFavorite> UserFavorite => Set<UserFavorite>();

  protected override void OnModelCreating(ModelBuilder modelBuilder)
  {
    modelBuilder.Entity<PropertyEstimateStats>().HasOne(tgp => tgp.Property).WithMany(m => m.EstimateStats)
      .HasForeignKey(tgp => tgp.PropertyId); base.OnModelCreating(modelBuilder);
    
    modelBuilder.Entity<PropertyImage>().HasOne(tgp => tgp.Property).WithMany(m => m.Images)
      .HasForeignKey(tgp => tgp.PropertyId); base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<PropertyPrediction>().HasOne(tgp => tgp.Miner).WithMany(m => m.Predictions)
        .HasForeignKey(tgp => tgp.MinerId);

    modelBuilder.Entity<PropertyPrediction>().HasOne(tgp => tgp.Validator).WithMany(m => m.Predictions)
        .HasForeignKey(tgp => tgp.ValidatorId);

    modelBuilder.Entity<PropertyPrediction>().HasOne(tgp => tgp.Property).WithMany(m => m.Predictions)
        .HasForeignKey(tgp => tgp.PropertyId); base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<MinerScore>().HasOne(tgp => tgp.Miner).WithMany(m => m.Scores)
        .HasForeignKey(tgp => tgp.MinerId);

    modelBuilder.Entity<MinerScore>().HasOne(tgp => tgp.Validator).WithMany(m => m.MinerScores)
      .HasForeignKey(tgp => tgp.ValidatorId);

    modelBuilder.Entity<MinerDatedScore>().HasOne(tgp => tgp.MinerScore).WithMany(m => m.MinerDatedScores)
      .HasForeignKey(tgp => tgp.MinerScoreId);

    modelBuilder.Entity<PropertyValuationPrediction>().HasOne(tgp => tgp.Miner).WithMany(m => m.ValuationPredictions)
        .HasForeignKey(tgp => tgp.MinerId);

    modelBuilder.Entity<PropertyValuationPrediction>().HasOne(tgp => tgp.Validator).WithMany(m => m.ValuationPredictions)
        .HasForeignKey(tgp => tgp.ValidatorId);

    modelBuilder.Entity<PropertyValuationPrediction>().HasOne(tgp => tgp.PropertyValuation).WithMany(m => m.Predictions)
        .HasForeignKey(tgp => tgp.PropertyValuationId); base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<UserSetting>().HasOne(tgp => tgp.User).WithMany(m => m.UserSettings)
      .HasForeignKey(tgp => tgp.UserId); base.OnModelCreating(modelBuilder);

    modelBuilder.Entity<UserFavorite>().HasOne(tgp => tgp.User).WithMany(m => m.UserFavorites)
      .HasForeignKey(tgp => tgp.UserId); base.OnModelCreating(modelBuilder);

    foreach (var entityType in modelBuilder.Model.GetEntityTypes())
    {
      modelBuilder.Entity(entityType.ClrType)
          .Ignore("Deleted")
          .Ignore("UpdatedAt")
          .Ignore("Version");
    }
  }

  public async Task SaveLogEntry(string apiName, string logEntry, string entryType, string executionInstanceId, string? ipAddress)
  {
    ApiLog.Add(new ApiLog
    {
      ApiName = apiName,
      LogEntry = logEntry,
      EntryType = entryType,
      TimeStamp = DateTime.UtcNow,
      ExecutionInstanceId = executionInstanceId,
      IpAddress = ipAddress
    });

    await SaveChangesAsync();
  }

  public async Task SaveLogEntry(string apiName, Exception ex, string executionInstanceId, string? ipAddress)
  {
    ApiLog.Add(new ApiLog
    {
      ApiName = apiName,
      LogEntry = ExceptionToString(ex),
      EntryType = "Error",
      TimeStamp = DateTime.UtcNow,
      ExecutionInstanceId = executionInstanceId,
      IpAddress = ipAddress
    });

    await SaveChangesAsync();
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