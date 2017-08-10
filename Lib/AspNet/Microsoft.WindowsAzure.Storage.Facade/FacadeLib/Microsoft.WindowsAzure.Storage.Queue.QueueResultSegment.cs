using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.Queue
{
public sealed class QueueResultSegment
{
    public IEnumerable<CloudQueue> Results
    {
        get; private set;
    }

    public QueueContinuationToken ContinuationToken
    {
        get; private set;
    }

    internal QueueResultSegment(IEnumerable<CloudQueue> queues, QueueContinuationToken continuationToken)
    {
        throw new System.NotImplementedException();
    }
}

}