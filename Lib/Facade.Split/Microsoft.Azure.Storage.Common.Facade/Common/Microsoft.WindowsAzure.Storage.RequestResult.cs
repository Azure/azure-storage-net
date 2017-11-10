using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
namespace Microsoft.Azure.Storage
{
public sealed class RequestResult
{

    public int HttpStatusCode
    {
        get; set;
    }

    public string HttpStatusMessage
    {
        get; internal set;
    }

    public string ServiceRequestID
    {
        get; internal set;
    }

    public string ContentMd5
    {
        get; internal set;
    }

    public string Etag
    {
        get; internal set;
    }

    public string RequestDate
    {
        get; internal set;
    }

    public StorageLocation TargetLocation
    {
        get; internal set;
    }

    public StorageExtendedErrorInformation ExtendedErrorInformation
    {
        get; internal set;
    }

    public bool IsRequestServerEncrypted
    {
        get; internal set;
    }

    public Exception Exception
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

    public static RequestResult TranslateFromExceptionMessage(string message)
    {
        throw new System.NotImplementedException();
    }
    internal string WriteAsXml()
    {
        throw new System.NotImplementedException();
    }
}

}