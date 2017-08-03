using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.Core
{
internal class SasQueryBuilder : UriQueryBuilder
{
    public bool RequireHttps
    {
        get; private set;
    }

    public SasQueryBuilder(string sasToken)
    {
        throw new System.NotImplementedException();
    }
    public override void Add(string name, string value)
    {
        throw new System.NotImplementedException();
    }
    public override Uri AddToUri(Uri uri)
    {
        throw new System.NotImplementedException();
    }
}

}