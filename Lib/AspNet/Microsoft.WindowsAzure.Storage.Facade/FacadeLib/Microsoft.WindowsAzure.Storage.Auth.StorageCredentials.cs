using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Globalization;
 
namespace Microsoft.WindowsAzure.Storage.Auth
{
public sealed class StorageCredentials
{

    public string SASToken
    {
        get; private set;
    }

    public string AccountName
    {
        get; private set;
    }

    public string KeyName
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal StorageAccountKey Key
    {
        get; private set;
    }

    public bool IsAnonymous
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public bool IsSAS
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public bool IsSharedKey
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public string SASSignature
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public StorageCredentials()
    {
        throw new System.NotImplementedException();
    }
    public StorageCredentials(string accountName, string keyValue)
      : this(accountName, keyValue, (string) null)
    {
        throw new System.NotImplementedException();
    }
    public StorageCredentials(string accountName, string keyValue, string keyName)
    {
        throw new System.NotImplementedException();
    }
    public StorageCredentials(string sasToken)
    {
        throw new System.NotImplementedException();
    }
    public void UpdateKey(string keyValue)
    {
        throw new System.NotImplementedException();
    }
    public void UpdateKey(string keyValue, string keyName)
    {
        throw new System.NotImplementedException();
    }
    public void UpdateSASToken(string sasToken)
    {
        throw new System.NotImplementedException();
    }
    public byte[] ExportKey()
    {
        throw new System.NotImplementedException();
    }
    public Uri TransformUri(Uri resourceUri)
    {
        throw new System.NotImplementedException();
    }
    public StorageUri TransformUri(StorageUri resourceUri)
    {
        throw new System.NotImplementedException();
    }
    public string ExportBase64EncodedKey()
    {
        throw new System.NotImplementedException();
    }
    private static string GetBase64EncodedKey(StorageAccountKey accountKey)
    {
        throw new System.NotImplementedException();
    }
    internal string ToString(bool exportSecrets)
    {
        throw new System.NotImplementedException();
    }
    public bool Equals(StorageCredentials other)
    {
        throw new System.NotImplementedException();
    }
    private void UpdateQueryBuilder()
    {
        throw new System.NotImplementedException();
    }
}

}