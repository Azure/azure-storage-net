using Microsoft.Azure.Storage.Core.Util;
using System;
 
namespace Microsoft.Azure.Storage.Core.Auth
{
internal sealed class SharedKeyTableCanonicalizer : ICanonicalizer
{
    private static SharedKeyTableCanonicalizer instance = new SharedKeyTableCanonicalizer();
    private const string SharedKeyAuthorizationScheme = "SharedKey";
    private const int ExpectedCanonicalizedStringLength = 200;

    public static SharedKeyTableCanonicalizer Instance
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public string AuthorizationScheme
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    private SharedKeyTableCanonicalizer()
    {
        throw new System.NotImplementedException();
    }
}

}