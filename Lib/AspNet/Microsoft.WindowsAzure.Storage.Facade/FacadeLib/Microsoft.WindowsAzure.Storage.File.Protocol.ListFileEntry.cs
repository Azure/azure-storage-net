using System;
using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
internal sealed class ListFileEntry : IListFileEntry
{
    internal CloudFileAttributes Attributes
    {
        get; private set;
    }

    public string Name
    {
        get; private set;
    }

    public FileProperties Properties
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public IDictionary<string, string> Metadata
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public Uri Uri
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal ListFileEntry(string name, CloudFileAttributes attributes)
    {
        throw new System.NotImplementedException();
    }
}

}