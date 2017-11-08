using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Xml;
namespace Microsoft.WindowsAzure.Storage
{
public sealed class ExceptionInfo
{
    public string Type
    {
        get; internal set;
    }

    public string Message
    {
        get; internal set;
    }

    public string Source
    {
        get; internal set;
    }

    public string StackTrace
    {
        get; internal set;
    }

    public ExceptionInfo InnerExceptionInfo
    {
        get; internal set;
    }

    public ExceptionInfo()
    {
        throw new System.NotImplementedException();
    }
    internal ExceptionInfo(Exception ex)
    {
        throw new System.NotImplementedException();
    }
    internal static ExceptionInfo ReadFromXMLReader(XmlReader reader)
    {
        throw new System.NotImplementedException();
    }
    internal void WriteXml(XmlWriter writer)
    {
        throw new System.NotImplementedException();
    }
    internal void ReadXml(XmlReader reader)
    {
        throw new System.NotImplementedException();
    }
}

}