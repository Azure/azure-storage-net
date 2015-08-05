// <copyright file="KeyVaultUtility.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

namespace EncryptionShared
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.Azure;
    using Microsoft.Azure.KeyVault;
    using Microsoft.IdentityModel.Clients.ActiveDirectory;

    /// <summary>
    /// Utility class for interacting with Key Vault.
    /// </summary>
    public static class KeyVaultUtility
    {
        /// <summary>
        /// Creates a secret in Azure Key Vault and returns its ID.
        /// </summary>
        /// <param name="secretName">The name of the secret to create.</param>
        /// <returns>The ID of the secret created.</returns>
        public static string SetUpKeyVaultSecret(string secretName)
        {
            KeyVaultClient cloudVault = new KeyVaultClient(GetAccessToken);
            string vaultUri = CloudConfigurationManager.GetSetting("VaultUri");

            try
            {
                // Delete the secret if it exists.
                cloudVault.DeleteSecretAsync(vaultUri, secretName).GetAwaiter().GetResult();
            }
            catch (KeyVaultClientException ex)
            {
                if (ex.Status != System.Net.HttpStatusCode.NotFound)
                {
                    Console.WriteLine("Unable to access the specified vault. Please confirm the KVClientId, KVClientKey, and VaultUri are valid in the app.config file.");
                    Console.WriteLine("Also ensure that the client ID has previously been granted full permissions for Key Vault secrets using the Set-AzureKeyVaultAccessPolicy command with the -PermissionsToSecrets parameter.");
                    Console.WriteLine("Press any key to exit");
                    Console.ReadLine();
                    throw;
                }
            }

            // Create a 256bit symmetric key and convert it to Base64.
            SymmetricKey symmetricKey = new SymmetricKey(secretName, SymmetricKey.KeySize256);
            string symmetricBytes = Convert.ToBase64String(symmetricKey.Key);

            // Store the Base64 of the key in the key vault. Note that the content-type of the secret must
            // be application/octet-stream or the KeyVaultKeyResolver will not load it as a key.
            Secret cloudSecret = cloudVault.SetSecretAsync(vaultUri, secretName, symmetricBytes, null, "application/octet-stream").GetAwaiter().GetResult();

            // Return the base identifier of the secret. This will be resolved to the current version of the secret.
            return cloudSecret.SecretIdentifier.BaseIdentifier;
        }

        /// <summary>
        /// Gets an access token for the client ID and client key specified in the configuration.
        /// </summary>
        /// <param name="authority">The authority granting access.</param>
        /// <param name="resource">The resource to access.</param>
        /// <param name="scope">The scope of the authentication request.</param>
        /// <returns>An access token.</returns>
        public static async Task<string> GetAccessToken(string authority, string resource, string scope)
        {
            ClientCredential credential = new ClientCredential(CloudConfigurationManager.GetSetting("KVClientId"), CloudConfigurationManager.GetSetting("KVClientKey"));

            AuthenticationContext ctx = new AuthenticationContext(new Uri(authority).AbsoluteUri, false);
            AuthenticationResult result = await ctx.AcquireTokenAsync(resource, credential);

            return result.AccessToken;
        }
    }
}
