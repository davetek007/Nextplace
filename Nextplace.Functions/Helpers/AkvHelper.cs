using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Extensions.Configuration;

namespace Nextplace.Functions.Helpers;

internal class AkvHelper(IConfiguration configuration)
{
    internal async Task<string> GetSecretAsync(string secretName)
    {
        var keyVaultUrl = configuration["KeyVaultUrl"];
        var userAssignedClientId = configuration["ManagedIdentityId"];

        var credentialOptions = new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = userAssignedClientId
        };

        var client = new SecretClient(new Uri(keyVaultUrl!), new DefaultAzureCredential(credentialOptions));
        KeyVaultSecret secret = await client.GetSecretAsync(secretName);
        return secret.Value;
    }
}