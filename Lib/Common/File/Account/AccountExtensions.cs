using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Core;
using System;

namespace Microsoft.WindowsAzure.Storage.File
{
    internal static class AccountExtensions
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
