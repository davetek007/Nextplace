using System.Security.Cryptography.X509Certificates;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;

namespace Nextplace.Api.Helpers;

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
    
    internal async Task<X509Certificate2> GetCertificateAsync(string certificateName)
    {
        var keyVaultUrl = configuration["KeyVaultUrl"];
        var userAssignedClientId = configuration["ManagedIdentityId"];

        var credentialOptions = new DefaultAzureCredentialOptions
        {
            ManagedIdentityClientId = userAssignedClientId
        };

        var credential = new DefaultAzureCredential(credentialOptions);

        var certificateClient = new CertificateClient(new Uri(keyVaultUrl!), credential);

        KeyVaultCertificateWithPolicy certificateWithPolicy = await certificateClient.GetCertificateAsync(certificateName);

        var secretUri = certificateWithPolicy.SecretId;

        var secretClient = new SecretClient(new Uri(keyVaultUrl!), credential);

        KeyVaultSecret secret;
        try
        {
            secret = await secretClient.GetSecretAsync(secretUri.AbsoluteUri);
        }
        catch
        {
            secret = await secretClient.GetSecretAsync(certificateName);
        }

        byte[] pfxBytes = Convert.FromBase64String(secret.Value);

        X509Certificate2 certificate = new X509Certificate2(pfxBytes, (string)null, X509KeyStorageFlags.Exportable);

        return certificate;
    }
}