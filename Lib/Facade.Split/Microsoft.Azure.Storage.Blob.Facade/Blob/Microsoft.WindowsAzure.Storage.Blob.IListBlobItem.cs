using System;
namespace Microsoft.Azure.Storage.Blob
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