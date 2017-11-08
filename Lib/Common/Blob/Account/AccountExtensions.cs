using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Core;
using System;

namespace Microsoft.WindowsAzure.Storage.Blob
{
    public static class BlobAccountExtensions
    {
        /// <summary>
        /// Creates the Blob service client.
        /// </summary>
        /// <returns>A <see cref="CloudBlobClient"/> object.</returns>
        public static CloudBlobClient CreateCloudBlobClient(this CloudStorageAccount account)
        {
            if (account.BlobEndpoint == null)
            {
                throw new InvalidOperationException(SR.BlobEndPointNotConfigured);
            }

            return new CloudBlobClient(account.BlobStorageUri, account.Credentials);
        }
    }
}
