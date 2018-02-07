using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.File;
using Microsoft.Azure.Storage.Core;
using System;

namespace Microsoft.Azure.Storage.File
{
    public static class FileAccountExtensions
    {
        /// <summary>
        /// Creates the File service client.
        /// </summary>
        /// <returns>A <see cref="CloudFileClient"/> object.</returns>
        public static CloudFileClient CreateCloudFileClient(this CloudStorageAccount account)
        {
            if (account.FileEndpoint == null)
            {
                throw new InvalidOperationException(SR.FileEndPointNotConfigured);
            }

            return new CloudFileClient(account.FileStorageUri, account.Credentials);
        }
    }
}
