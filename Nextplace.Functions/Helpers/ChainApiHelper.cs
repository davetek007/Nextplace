using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Nextplace.Functions.Helpers;

internal class ChainApiHelper(IConfiguration configuration)
{
    private async Task<List<DataItem>> GetMetagraph()
    {
        var apiKey = await new AkvHelper(configuration).GetSecretAsync("TaostatsApiKey");
        var url = configuration["TaostatsApiUrl"];

        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", apiKey);
        var response = await httpClient.GetAsync(url);

        response.EnsureSuccessStatusCode();

        var responseBody = await response.Content.ReadAsStringAsync();
        var rootObject = JsonConvert.DeserializeObject<RootObject>(responseBody);

        return rootObject == null ? [] : rootObject.Data;
    }

    internal async Task<List<DataItem>> GetValidators()
    {
        var metagraph = await GetMetagraph();

        var items = new List<DataItem>();
        foreach (var item in metagraph.Where(item => item.ValidatorTrust != 0))
        {
            if (item.Axon == null || string.IsNullOrWhiteSpace(item.Axon.Ip))
            {
                continue;
            }

            items.Add(item);
        }

        return items;
    }

    internal async Task<List<DataItem>> GetMiners()
    {
        var metagraph = await GetMetagraph();

        var items = new List<DataItem>();
        foreach (var item in metagraph.Where(item => item.ValidatorTrust == 0))
        {
            items.Add(item);
        }

        return items;
    }
}