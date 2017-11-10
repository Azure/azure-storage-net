using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Collections.Generic;
namespace Microsoft.Azure.Storage.Core
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