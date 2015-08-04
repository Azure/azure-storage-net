using Microsoft.Azure.KeyVault;
using Microsoft.Azure.KeyVault.Core;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.WindowsAzure;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KVGettingStarted
{
    /// <summary>
    /// Demonstrates how to use encryption along with Azure Key Vault integration for the Azure Blob service.
    /// </summary>
    public class Program
    {
        const string DemoContainer = "democontainer";

        static async Task<string> GetAccessToken(string authority, string resource, string scope)
        {
            ClientCredential credential = new ClientCredential(CloudConfigurationManager.GetSetting("KVClientId"), CloudConfigurationManager.GetSetting("KVClientKey"));

            AuthenticationContext ctx = new AuthenticationContext(new Uri(authority).AbsoluteUri, false);
            AuthenticationResult result = await ctx.AcquireTokenAsync(resource, credential);

            return result.AccessToken;
        }

        static void Main(string[] args)
        {
            Console.WriteLine("Blob encryption with Key Vault integration");

            // Retrieve storage account information from connection string
            // How to create a storage connection string - http://msdn.microsoft.com/en-us/library/azure/ee758697.aspx
            CloudStorageAccount storageAccount = CreateStorageAccountFromConnectionString(CloudConfigurationManager.GetSetting("StorageConnectionString"));
            CloudBlobClient client = storageAccount.CreateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference(DemoContainer + Guid.NewGuid().ToString("N"));

            // Get reference to a Cloud Key Vault and key resolver.
            KeyVaultClient cloudVault = new KeyVaultClient(GetAccessToken);
            KeyVaultKeyResolver cloudResolver = new KeyVaultKeyResolver(GetAccessToken);

            // Get reference to a local key and key resolver.
            RsaKey rsaKey = new RsaKey("rsakey");
            LocalResolver resolver = new LocalResolver();
            resolver.Add(rsaKey);

            // If there are multiple key sources like Azure Key Vault and local KMS, set up an aggregate resolver as follows.
            // This helps users to define a plugin model for all the different key providers they support.
            AggregateKeyResolver aggregateResolver = new AggregateKeyResolver()
                .Add(resolver)
                .Add(cloudResolver);

            // Set up a caching resolver so the secrets can be cached on the client. This is the recommended usage pattern since the throttling
            // targets for Storage and Key Vault services are orders of magnitude different.
            CachingKeyResolver cachingResolver = new CachingKeyResolver(2, aggregateResolver);
            
            // Establish a symmetric KEK stored as a Secret in the cloud key vault
            string vaultUri = CloudConfigurationManager.GetSetting("VaultUri");

            try
            {
                cloudVault.DeleteSecretAsync(vaultUri, "secret").GetAwaiter().GetResult();
            }
            catch (KeyVaultClientException ex)
            {
                if (ex.Status != System.Net.HttpStatusCode.NotFound)
                    throw;
            }

            // Create a symmetric 256bit symmetric key and convert it to Base64
            SymmetricKey symmetricKey = new SymmetricKey("secret", SymmetricKey.KeySize256);
            string symmetricBytes = Convert.ToBase64String(symmetricKey.Key);

            // Store the Base64 of the key in the key vault. This is shown inline for simplicity but
            // the recommended approach is to create this key offline and upload it to key vault and
            // then use secrets base identifier as a parameter to resolve the current version of the
            // secret for encryption. Note that the content-type of the secret must
            // be application/octet-stream or the KeyVaultKeyResolver will refuse to load it as a key.
            Secret cloudSecret = cloudVault.SetSecretAsync(vaultUri, "secret", symmetricBytes, null, "application/octet-stream").GetAwaiter().GetResult();
            IKey cloudKey = cachingResolver.ResolveKeyAsync(cloudSecret.SecretIdentifier.BaseIdentifier, CancellationToken.None).GetAwaiter().GetResult();

            try
            {
                container.Create();
                int size = 5 * 1024 * 1024;
                byte[] buffer = new byte[size];

                Random rand = new Random();
                rand.NextBytes(buffer);

                // Upload first blob using the secret stored in Azure Key Vault.
                CloudBlockBlob blob = container.GetBlockBlobReference("blockblob1");

                // Create the encryption policy using the secret stored in Azure Key Vault to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(cloudKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                Console.WriteLine("Uploading the 1st encrypted blob.");

                // Upload the encrypted contents to the blob.
                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    blob.UploadFromStream(stream, size, null, uploadOptions, null);
                }

                // Download the encrypted blob.
                BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, cachingResolver);

                // Set the decryption policy on the request options.
                BlobRequestOptions downloadOptions = new BlobRequestOptions() { EncryptionPolicy = downloadPolicy };

                Console.WriteLine("Downloading the 1st encrypted blob.");

                // Download and decrypt the encrypted contents from the blob.
                using (MemoryStream outputStream = new MemoryStream())
                {
                    blob.DownloadToStream(outputStream, null, downloadOptions, null);
                }

                // Upload second blob using the local key.
                blob = container.GetBlockBlobReference("blockblob2");

                // Create the encryption policy using the local key.
                uploadPolicy = new BlobEncryptionPolicy(rsaKey, null);

                // Set the encryption policy on the request options.
                uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                Console.WriteLine("Uploading the 2nd encrypted blob.");

                // Upload the encrypted contents to the blob.
                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    blob.UploadFromStream(stream, size, null, uploadOptions, null);
                }

                // Download the encrypted blob. The same policy and options created before can be used because the aggregate resolver contains both
                // resolvers and will pick the right one based on the key id stored in blob metadata on the service.
                Console.WriteLine("Downloading the 2nd encrypted blob.");

                // Download and decrypt the encrypted contents from the blob.
                using (MemoryStream outputStream = new MemoryStream())
                {
                    blob.DownloadToStream(outputStream, null, downloadOptions, null);
                }

                Console.WriteLine("Press enter key to exit"); 
                Console.ReadLine();
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        private static CloudStorageAccount CreateStorageAccountFromConnectionString(string storageConnectionString)
        {
            CloudStorageAccount storageAccount;
            try
            {
                storageAccount = CloudStorageAccount.Parse(storageConnectionString);
            }
            catch (FormatException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
                throw;
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Invalid storage account information provided. Please confirm the AccountName and AccountKey are valid in the app.config file - then restart the sample.");
                Console.WriteLine("Press any key to exit");
                Console.ReadLine();
                throw;
            }

            return storageAccount;
        }
    }
}
