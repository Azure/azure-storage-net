using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.IO;
namespace Microsoft.Azure.Storage.Queue.Protocol
{
internal sealed class GetMessagesResponse : ResponseParsingBase<QueueMessage>
{
    public IEnumerable<QueueMessage> Messages
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public GetMessagesResponse(Stream stream)
      : base(stream)
    {
        throw new System.NotImplementedException();
    }
    private QueueMessage ParseMessageEntry()
    {
        throw new System.NotImplementedException();
    }
    protected override IEnumerable<QueueMessage> ParseXml()
    {
        throw new System.NotImplementedException();
    }
}

}