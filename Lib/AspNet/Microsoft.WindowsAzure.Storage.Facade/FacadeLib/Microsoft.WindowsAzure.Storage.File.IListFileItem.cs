using System;
namespace Microsoft.WindowsAzure.Storage.File
{
public interface IListFileItem
{
    Uri Uri
    {
        get;
    }

    StorageUri StorageUri
    {
        get;
    }

    CloudFileDirectory Parent
    {
        get;
    }

    CloudFileShare Share
    {
        get;
    }
}

}