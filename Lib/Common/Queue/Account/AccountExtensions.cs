using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.Storage.Core;
using System;

namespace Microsoft.Azure.Storage.Queue
{
    public static class QueueAccountExtensions
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
