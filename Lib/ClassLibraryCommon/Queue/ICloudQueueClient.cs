namespace Microsoft.WindowsAzure.Storage.Queue
{
    /// <summary>
    /// The cloud queue client.
    /// </summary>
    public interface ICloudQueueClient
    {
        /// <summary>
        /// Returns a reference to a <see cref="CloudQueue"/> object with the specified name.
        /// </summary>
        /// <param name="queueName">A string containing the name of the queue.</param>
        /// <returns>A <see cref="CloudQueue"/> object.</returns>
        CloudQueue GetQueueReference(string queueName);
    }
}