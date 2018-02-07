using System.Collections.Generic;
namespace Microsoft.Azure.Storage.Shared.Protocol
{
public sealed class CorsRule
{

    public IList<string> AllowedOrigins
    {
        get
        {
            throw new System.NotImplementedException();
        }
        set
        {
            throw new System.NotImplementedException();
        }
    }

    public IList<string> ExposedHeaders
    {
        get
        {
            throw new System.NotImplementedException();
        }
        set
        {
            throw new System.NotImplementedException();
        }
    }

    public IList<string> AllowedHeaders
    {
        get
        {
            throw new System.NotImplementedException();
        }
        set
        {
            throw new System.NotImplementedException();
        }
    }

    public CorsHttpMethods AllowedMethods
    {
        get; set;
    }

    public int MaxAgeInSeconds
    {
        get; set;
    }
}

}