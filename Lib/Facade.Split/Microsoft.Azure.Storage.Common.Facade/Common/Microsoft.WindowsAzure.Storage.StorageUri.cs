using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.RetryPolicies;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace Microsoft.Azure.Storage
{
public sealed class StorageUri
{

    public Uri PrimaryUri
    {
        get
        {
            throw new System.NotImplementedException();
        }
        private set
        {
            throw new System.NotImplementedException();
        }
    }

    public Uri SecondaryUri
    {
        get
        {
            throw new System.NotImplementedException();
        }
        private set
        {
            throw new System.NotImplementedException();
        }
    }

    public StorageUri(Uri primaryUri)
      : this(primaryUri, (Uri) null)
    {
        throw new System.NotImplementedException();
    }
    public StorageUri(Uri primaryUri, Uri secondaryUri)
    {
        throw new System.NotImplementedException();
    }
    public static bool operator ==(StorageUri uri1, StorageUri uri2)
    {
        throw new System.NotImplementedException();
    }
    public static bool operator !=(StorageUri uri1, StorageUri uri2)
    {
        throw new System.NotImplementedException();
    }
    public Uri GetUri(StorageLocation location)
    {
        throw new System.NotImplementedException();
    }
    internal bool ValidateLocationMode(LocationMode mode)
    {
        throw new System.NotImplementedException();
    }
    public override string ToString()
    {
        throw new System.NotImplementedException();
    }
    public override int GetHashCode()
    {
        throw new System.NotImplementedException();
    }
    public override bool Equals(object obj)
    {
        throw new System.NotImplementedException();
    }
    public bool Equals(StorageUri other)
    {
        throw new System.NotImplementedException();
    }
    private static void AssertAbsoluteUri(Uri uri)
    {
        throw new System.NotImplementedException();
    }
}

}