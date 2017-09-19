using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Core;
using System;

namespace Microsoft.WindowsAzure.Storage.Queue
{
    internal static class AccountExtensions
    {
        /// <summary>
        /// Creates the Queue service client.
        /// </summary>
        /// <returns>A <see cref="CloudQueueClient"/> object.</returns>
        public static CloudQueueClient CreateCloudQueueClient(this CloudStorageAccount account)
        {
            if (account.QueueEndpoint == null)
            {
                throw new InvalidOperationException(SR.QueueEndPointNotConfigured);
            }

            return new CloudQueueClient(account.QueueStorageUri, account.Credentials);
        }
    }
}
