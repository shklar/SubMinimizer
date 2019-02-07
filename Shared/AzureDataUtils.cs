using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Web.Helpers;
using CogsMinimizer.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using Microsoft.Azure.Management.Authorization;
using Microsoft.Azure.Management.Authorization.Models;
using Microsoft.Azure.Management.ResourceManager;
using Microsoft.Azure.Management.ResourceManager.Models;
using Microsoft.Rest;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Azure.KeyVault;
using System.Threading.Tasks;

namespace CogsMinimizer.Shared
{
    public static class AzureDataUtils
    {
        public static Provider CreateProvider(string content)
        {
            JObject jObject = JObject.Parse(content);
            System.Web.Helpers.DynamicJsonObject providerResult = Json.Decode(content);
            JArray jresourceTypes = (JArray)jObject["resourceTypes"];

            List<ProviderResourceType> resourceTypes = new List<ProviderResourceType>();
            foreach (JObject jresourceType in jresourceTypes)
            {
                ProviderResourceType resourceType = new ProviderResourceType();
                resourceType.ResourceType = (string)jresourceType["resourceType"];
                resourceTypes.Add(resourceType);

                List<string> resourceTypeApiVersions = new List<string>();
                foreach (var japiVersion in (JArray)(jresourceType["apiVersions"]))
                {
                    resourceTypeApiVersions.Add((string)japiVersion);
                }

                resourceTypeApiVersions.Sort(delegate (string v1, string v2)
                {
                    try
                    {
                        DateTime d1 = DateTime.Parse(v1.Replace("-preview", ""));
                        DateTime d2 = DateTime.Parse(v2.Replace("-preview", ""));
                        return d1 > d2 ? 1 : 0;
                    }
                    catch (Exception)
                    {
                        // Some value parsing to date failed.
                        return 0;
                    }
                });

                resourceType.ApiVersions = resourceTypeApiVersions;
            }

            Provider provider = new Provider((string)jObject["id"], (string)jObject["namespace"], null, resourceTypes);
            return provider;

        }

        public static string GetKeyVaultSecret(string keyVaultName, string secretName)
        {
            AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
            var keyVaultClient = new KeyVaultClient(new Microsoft.Azure.KeyVault.KeyVaultClient.AuthenticationCallback(azureServiceTokenProvider.KeyVaultTokenCallback));
            Task<Microsoft.Azure.KeyVault.Models.SecretBundle> task = keyVaultClient.GetSecretAsync(
                $"https://{keyVaultName}.vault.azure.net/secrets/{secretName}");
            task.Wait();

            return task.Result.Value;
        }

    }
}
