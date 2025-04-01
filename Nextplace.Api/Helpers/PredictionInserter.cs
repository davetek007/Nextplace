using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Nextplace.Api.Db;

namespace Nextplace.Api.Helpers;

public static class PredictionInserter
{
  private const int MaxParamsPerBatch = 2100;
  private const int ParamsPerRow = 8;
  private const int MaxRowsPerBatch = MaxParamsPerBatch / ParamsPerRow;

  public static async Task InsertPredictionsAsync(AppDbContext context, List<PropertyPrediction> predictions, string executionInstanceId)
  {
    var groups = predictions.GroupBy(p => p.PredictionDate.Date);

    foreach (var group in groups)
    {
      var tableName = $"PropertyPrediction{group.Key:yyyy-MM-dd}";
      await EnsureTableExistsAsync(context, tableName);

      var rows = group.ToList();
      for (var i = 0; i < rows.Count; i += MaxRowsPerBatch)
      {
        var batch = rows.Skip(i).Take(MaxRowsPerBatch).ToList();
        await InsertBatchAsync(context, tableName, batch);
      }
      
      await context.SaveLogEntry("PostPredictions",
        $"{tableName}: Inserted {rows.Count}", "Information", executionInstanceId);

    }
  }

  private static async Task InsertBatchAsync(AppDbContext context, string tableName, List<PropertyPrediction> batch)
  {
    var sb = new StringBuilder();
    var parameters = new List<SqlParameter>();

    sb.Append($"INSERT INTO [{tableName}] " +
              "([MinerId], [ValidatorId], [PredictedSaleDate], [PredictedSalePrice], [PredictionScore], [PredictionDate], [PropertyId], [CreateDate]) VALUES ");

    for (var i = 0; i < batch.Count; i++)
    {
      var p = batch[i];
      if (i > 0) sb.Append(",");

      sb.Append($"(@MinerId{i}, @ValidatorId{i}, @PredictedSaleDate{i}, @PredictedSalePrice{i}, @PredictionScore{i}, @PredictionDate{i}, @PropertyId{i}, @CreateDate{i})");

      parameters.AddRange(
      [
        new SqlParameter($"@MinerId{i}", p.MinerId),
        new SqlParameter($"@ValidatorId{i}", (object?)p.ValidatorId ?? DBNull.Value),
        new SqlParameter($"@PredictedSaleDate{i}", p.PredictedSaleDate),
        new SqlParameter($"@PredictedSalePrice{i}", p.PredictedSalePrice),
        new SqlParameter($"@PredictionScore{i}", (object?)p.PredictionScore ?? DBNull.Value),
        new SqlParameter($"@PredictionDate{i}", p.PredictionDate),
        new SqlParameter($"@PropertyId{i}", p.PropertyId),
        new SqlParameter($"@CreateDate{i}", p.CreateDate)
      ]);
    }

    // ReSharper disable once CoVariantArrayConversion
    await context.Database.ExecuteSqlRawAsync(sb.ToString(), parameters.ToArray());
  }

  private static async Task EnsureTableExistsAsync(AppDbContext context, string tableName)
  {
    var sql = $@"
      IF NOT EXISTS (SELECT * FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}')
      BEGIN
          CREATE TABLE [{tableName}] (
              [id] BIGINT IDENTITY(1,1) PRIMARY KEY NOT NULL,
              [propertyId] BIGINT NOT NULL,
              [minerId] BIGINT NOT NULL,
              [predictionDate] DATETIME2 NOT NULL,
              [predictedSaleDate] DATETIME2 NOT NULL,
              [predictedSalePrice] FLOAT NOT NULL,
              [createDate] DATETIME2 NOT NULL,
              [predictionScore] FLOAT NULL,
              [validatorId] BIGINT NULL
          );
      END";

    await context.Database.ExecuteSqlRawAsync(sql);
  }
}