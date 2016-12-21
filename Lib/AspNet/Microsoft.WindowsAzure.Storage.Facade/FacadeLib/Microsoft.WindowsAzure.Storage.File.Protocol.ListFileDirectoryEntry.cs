using System;
namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
internal sealed class ListFileDirectoryEntry : IListFileEntry
{
    public string Name
    {
        get; internal set;
    }

    public Uri Uri
    {
        get; internal set;
    }

    public FileDirectoryProperties Properties
    {
        get; internal set;
    }

    internal ListFileDirectoryEntry(string name, Uri uri, FileDirectoryProperties properties)
    {
        throw new System.NotImplementedException();
    }
}

}