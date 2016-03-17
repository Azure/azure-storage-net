namespace Microsoft.WindowsAzure.Storage
{
    using Microsoft.WindowsAzure.Storage.Analytics;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.File;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;

    public interface ICloudStorageAccount
    {
        /// <summary>
        /// Creates the Table service client.
        /// </summary>
        /// <returns>A <see cref="CloudTableClient"/> object.</returns>
        CloudTableClient CreateCloudTableClient();

        /// <summary>
        /// Creates the Queue service client.
        /// </summary>
        /// <returns>A <see cref="CloudQueueClient"/> object.</returns>
        CloudQueueClient CreateCloudQueueClient();

        /// <summary>
        /// Creates the Blob service client.
        /// </summary>
        /// <returns>A <see cref="CloudBlobClient"/> object.</returns>
        CloudBlobClient CreateCloudBlobClient();

        /// <summary>
        /// Creates an analytics client.
        /// </summary>
        /// <returns>A <see cref="CloudAnalyticsClient"/> object.</returns>
        CloudAnalyticsClient CreateCloudAnalyticsClient();

        /// <summary>
        /// Creates the File service client.
        /// </summary>
        /// <returns>A client object that specifies the File service endpoint.</returns>
        CloudFileClient CreateCloudFileClient();
    }
}