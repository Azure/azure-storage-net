using System;
namespace Microsoft.WindowsAzure.Storage.Blob
{
public interface IListBlobItem
{
    Uri Uri
    {
        get;
    }

    StorageUri StorageUri
    {
        get;
    }

    CloudBlobDirectory Parent
    {
        get;
    }

    CloudBlobContainer Container
    {
        get;
    }
}

}