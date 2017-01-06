using Microsoft.Data.OData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
 
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.Table.Protocol
{
internal class HttpResponseAdapterMessage : IODataResponseMessage
{
    public IEnumerable<KeyValuePair<string, string>> Headers
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public int StatusCode
    {
        get
        {
            throw new System.NotImplementedException();
        }
        set
        {
            throw new System.NotImplementedException();
        }
    }

    public Task<Stream> GetStreamAsync()
    {
        throw new System.NotImplementedException();
    }
    public string GetHeader(string headerName)
    {
        throw new System.NotImplementedException();
    }
    public Stream GetStream()
    {
        throw new System.NotImplementedException();
    }
    public void SetHeader(string headerName, string headerValue)
    {
        throw new System.NotImplementedException();
    }
}

}