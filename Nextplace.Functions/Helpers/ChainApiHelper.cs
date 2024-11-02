using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Nextplace.Functions.Helpers;

internal class ChainApiHelper(IConfiguration configuration)
{
    private async Task<List<Item>> GetMetagraph()
    {
        var apiKey = await new AkvHelper(configuration).GetSecretAsync("TaostatsApiKey");
        var url = configuration["TaostatsApiUrl"];

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", apiKey);
        var response = await httpClient.GetAsync(url);

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var rootObject = JsonConvert.DeserializeObject<Root>(responseBody);

        return rootObject == null ? [] : rootObject.Items;
    }

    internal async Task<List<Item>> GetValidators()
    {
        var metagraph = await GetMetagraph();

        var items = new List<Item>();
        foreach (var item in metagraph.Where(item => item.ValidatorTrust != 0))
        {
            if (item.AxonInfo == null || string.IsNullOrWhiteSpace(item.AxonInfo.Ip))
            {
                continue;
            }

            items.Add(item);
        }

        return items;
    }

    internal async Task<List<Item>> GetMiners()
    {
        var metagraph = await GetMetagraph();

        var items = new List<Item>();
        foreach (var item in metagraph.Where(item => item.ValidatorTrust == 0))
        {
            if (item.AxonInfo == null || string.IsNullOrWhiteSpace(item.AxonInfo.Ip))
            {
                continue;
            }

            items.Add(item);
        }

        return items;
    }
}