using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Users.Item.SendMail;
using Nextplace.Functions.Db;
using Nextplace.Functions.Helpers;
using Nextplace.Functions.Models;

namespace Nextplace.Functions.Functions;

public sealed class CalculatePropertyValuations(ILoggerFactory loggerFactory, AppDbContext context, IConfiguration configuration)
{
    private readonly ILogger _logger = loggerFactory.CreateLogger<CalculatePropertyEstimateStats>(); 

    [Function("CalculatePropertyValuations")]
    public async Task Run([TimerTrigger("%CalculatePropertyValuationsTimerSchedule%")] TimerInfo myTimer)
    { 
        var executionInstanceId = Guid.NewGuid().ToString();
        
        try
        {
            _logger.LogInformation($"CalculatePropertyValuations executed at: {DateTime.UtcNow}");
            await context.SaveLogEntry("CalculatePropertyValuations", "Started", "Information", executionInstanceId);

            var propertyValuations = await context.PropertyValuation
                .Include(pv => pv.Predictions!)
                .ThenInclude(pv => pv.Miner)
                .Where(pv => pv.Active && pv.RequestStatus == "New")
                .ToListAsync();

            foreach (var propertyValuation in propertyValuations)
            {
                var predictions = propertyValuation.Predictions!
                    .Where(p => p.Active && p.CreateDate < DateTime.UtcNow.AddSeconds(-5))
                    .ToList();
                
                if(predictions.Count == 0)
                {
                    continue;
                }

                var topMinerPredictions = predictions
                    .OrderByDescending(p => p.Miner.Incentive)
                    .Take(10)
                    .ToList();

                var predictedValue = propertyValuation.ProposedListingPrice.ToString("C2");
                var minSalePrice = topMinerPredictions.Min(p => p.PredictedSalePrice).ToString("C2");
                var maxSalePrice = topMinerPredictions.Max(p => p.PredictedSalePrice).ToString("C2");
                var avg = topMinerPredictions.Average(p => p.PredictedSalePrice);
                var avgSalePrice = topMinerPredictions.Average(p => p.PredictedSalePrice).ToString("C2");
                var underOver = "neither under nor overvalued";
                if (propertyValuation.ProposedListingPrice > avg)
                {
                    underOver = "overvalued";
                }
                else if (propertyValuation.ProposedListingPrice < avg)
                {
                    underOver = "undervalued";
                }

                const string notProvided= @"Not provided";
                var requestStatus = "Completed";
                try
                {
                    await SendEmail(propertyValuation.RequestorEmailAddress, predictedValue, minSalePrice, maxSalePrice,
                        avgSalePrice,
                        propertyValuation.City ?? notProvided, propertyValuation.State ?? notProvided,
                        propertyValuation.ZipCode ?? notProvided,
                        propertyValuation.Address ?? notProvided,
                        (propertyValuation.NumberOfBeds.HasValue
                            ? propertyValuation.NumberOfBeds.ToString()
                            : notProvided)!,
                        (propertyValuation.NumberOfBaths.HasValue
                            ? propertyValuation.NumberOfBaths.ToString()
                            : notProvided)!,
                        (propertyValuation.SquareFeet.HasValue
                            ? propertyValuation.SquareFeet.ToString()
                            : notProvided)!,
                        (propertyValuation.LotSize.HasValue ? propertyValuation.LotSize.ToString() : notProvided)!,
                        (propertyValuation.YearBuilt.HasValue ? propertyValuation.YearBuilt.ToString() : notProvided)!,
                        (propertyValuation.HoaDues.HasValue ? propertyValuation.HoaDues.ToString() : notProvided)!, underOver);
                }
                catch(Exception ex)
                {
                    requestStatus = "Error";
                    await context.SaveLogEntry("CalculatePropertyValuations", new Exception("Error sending email", ex), executionInstanceId);
                }

                propertyValuation.RequestStatus = requestStatus;
                propertyValuation.LastUpdateDate = DateTime.UtcNow;

                await context.SaveChangesAsync();
            }

            if (myTimer.ScheduleStatus is not null)
            {
                _logger.LogInformation(
                    $"Next timer for CalculatePropertyValuations is schedule at: {myTimer.ScheduleStatus.Next}");
            }
            
            await context.SaveLogEntry("CalculatePropertyValuations", "Completed", "Information", executionInstanceId);
        }
        catch (Exception ex)
        {
            await context.SaveLogEntry("CalculatePropertyValuations", ex, executionInstanceId);
        }
    }
    
    private async Task SendEmail(string emailAddress, string predictedValue, string minValue, string maxValue, string avgValue, string city, string state,
        string zipCode, string address, string numberOfBeds, string numberOfBaths, string squareFeet,
        string lotSize, string yearBuilt, string hoaDues, string underOver)
    {
        var akv = new AkvHelper(configuration);

        var emailTenantId = await akv.GetSecretAsync("EmailTenantId");
        var emailClientSecret = await akv.GetSecretAsync("EmailClientSecret");
        var emailClientId = await akv.GetSecretAsync("EmailClientId");

        var clientSecretCredential = new ClientSecretCredential(emailTenantId, emailClientId, emailClientSecret);

        var graphClient = new GraphServiceClient(clientSecretCredential);

        var message = new SendMailPostRequestBody
        {
            Message = new Message
            {
                Subject = "Your Property Valuation",
                Body = new ItemBody
                {
                    ContentType = BodyType.Html,
                    Content = EmailContent.PropertyValuation(predictedValue, minValue, maxValue, avgValue, city, state, zipCode,
                        address, numberOfBeds, numberOfBaths, squareFeet, lotSize, yearBuilt, hoaDues, underOver)
                },
                ToRecipients = new List<Recipient>
                {
                    new()
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = emailAddress
                        }
                    }
                }
            }
        };

        EmailContent.AddHeaderImage(message.Message);

        await graphClient.Users["admin@nextplace.ai"].SendMail.PostAsync(message);
    }


}