using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Auth;
using Microsoft.WindowsAzure.Storage.Core.Executor;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.Queue.Protocol;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
 
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.Queue
{
public class CloudQueue
{

    public CloudQueueClient ServiceClient
    {
        get; private set;
    }

    public Uri Uri
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public StorageUri StorageUri
    {
        get; private set;
    }

    public string Name
    {
        get; private set;
    }

    public int? ApproximateMessageCount
    {
        get; private set;
    }

    public bool EncodeMessage
    {
        get; set;
    }

    public IDictionary<string, string> Metadata
    {
        get; private set;
    }

    public CloudQueue(Uri queueAddress)
      : this(queueAddress, (StorageCredentials) null)
    {
        throw new System.NotImplementedException();
    }
    public CloudQueue(Uri queueAddress, StorageCredentials credentials)
      : this(new StorageUri(queueAddress), credentials)
    {
        throw new System.NotImplementedException();
    }
    public CloudQueue(StorageUri queueAddress, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
    internal CloudQueue(string queueName, CloudQueueClient serviceClient)
      : this((IDictionary<string, string>) new Dictionary<string, string>(), queueName, serviceClient)
    {
        throw new System.NotImplementedException();
    }
    internal CloudQueue(IDictionary<string, string> metadata, string queueName, CloudQueueClient serviceClient)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync(QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task CreateAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> CreateIfNotExistsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> CreateIfNotExistsAsync(QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> CreateIfNotExistsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteAsync(QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> DeleteIfExistsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> DeleteIfExistsAsync(QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> DeleteIfExistsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPermissionsAsync(QueuePermissions permissions)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPermissionsAsync(QueuePermissions permissions, QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetPermissionsAsync(QueuePermissions permissions, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<QueuePermissions> GetPermissionsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<QueuePermissions> GetPermissionsAsync(QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<QueuePermissions> GetPermissionsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> ExistsAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> ExistsAsync(QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<bool> ExistsAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task FetchAttributesAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task FetchAttributesAsync(QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task FetchAttributesAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetMetadataAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetMetadataAsync(QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task SetMetadataAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task AddMessageAsync(CloudQueueMessage message)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task AddMessageAsync(CloudQueueMessage message, TimeSpan? timeToLive, TimeSpan? initialVisibilityDelay, QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task AddMessageAsync(CloudQueueMessage message, TimeSpan? timeToLive, TimeSpan? initialVisibilityDelay, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task UpdateMessageAsync(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteMessageAsync(CloudQueueMessage message)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteMessageAsync(CloudQueueMessage message, QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteMessageAsync(string messageId, string popReceipt)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteMessageAsync(string messageId, string popReceipt, QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task DeleteMessageAsync(string messageId, string popReceipt, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int messageCount)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<IEnumerable<CloudQueueMessage>> GetMessagesAsync(int messageCount, TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudQueueMessage> GetMessageAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudQueueMessage> GetMessageAsync(TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudQueueMessage> GetMessageAsync(TimeSpan? visibilityTimeout, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<IEnumerable<CloudQueueMessage>> PeekMessagesAsync(int messageCount)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<IEnumerable<CloudQueueMessage>> PeekMessagesAsync(int messageCount, QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<IEnumerable<CloudQueueMessage>> PeekMessagesAsync(int messageCount, QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudQueueMessage> PeekMessageAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudQueueMessage> PeekMessageAsync(QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task<CloudQueueMessage> PeekMessageAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ClearAsync()
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ClearAsync(QueueRequestOptions options, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    [DoesServiceRequest]
    public virtual Task ClearAsync(QueueRequestOptions options, OperationContext operationContext, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> CreateQueueImpl(QueueRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> DeleteQueueImpl(QueueRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> FetchAttributesImpl(QueueRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<bool> ExistsImpl(QueueRequestOptions options, bool primaryOnly)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetMetadataImpl(QueueRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> SetPermissionsImpl(QueuePermissions acl, QueueRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<QueuePermissions> GetPermissionsImpl(QueueRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> AddMessageImpl(CloudQueueMessage message, TimeSpan? timeToLive, TimeSpan? initialVisibilityDelay, QueueRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> UpdateMessageImpl(CloudQueueMessage message, TimeSpan visibilityTimeout, MessageUpdateFields updateFields, QueueRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<NullType> DeleteMessageImpl(string messageId, string popReceipt, QueueRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<IEnumerable<CloudQueueMessage>> GetMessagesImpl(int messageCount, TimeSpan? visibilityTimeout, QueueRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    private RESTCommand<IEnumerable<CloudQueueMessage>> PeekMessagesImpl(int messageCount, QueueRequestOptions options)
    {
        throw new System.NotImplementedException();
    }
    internal StorageUri GetMessageRequestAddress()
    {
        throw new System.NotImplementedException();
    }
    internal StorageUri GetIndividualMessageAddress(string messageId)
    {
        throw new System.NotImplementedException();
    }
    private void ParseQueryAndVerify(StorageUri address, StorageCredentials credentials)
    {
        throw new System.NotImplementedException();
    }
    private string GetCanonicalName()
    {
        throw new System.NotImplementedException();
    }
    private static CloudQueueMessage SelectAddMessageResponse(CloudQueueMessage message, QueueMessage protocolMessage)
    {
        throw new System.NotImplementedException();
    }
    private CloudQueueMessage SelectGetMessageResponse(QueueMessage protocolMessage, QueueRequestOptions options = null)
    {
        throw new System.NotImplementedException();
    }
    private CloudQueueMessage SelectPeekMessageResponse(QueueMessage protocolMessage, QueueRequestOptions options = null)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessQueuePolicy policy)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessQueuePolicy policy, string accessPolicyIdentifier)
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessQueuePolicy policy, string accessPolicyIdentifier, SharedAccessProtocol? protocols, IPAddressOrRange ipAddressOrRange)
    {
        throw new System.NotImplementedException();
    }
}

}