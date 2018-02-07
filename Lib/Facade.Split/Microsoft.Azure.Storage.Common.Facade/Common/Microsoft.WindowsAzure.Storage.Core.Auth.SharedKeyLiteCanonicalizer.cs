using Microsoft.Azure.Storage.Core.Util;
using System;
 
namespace Microsoft.Azure.Storage.Core.Auth
{
internal sealed class SharedKeyLiteCanonicalizer : ICanonicalizer
{
    private static SharedKeyLiteCanonicalizer instance = new SharedKeyLiteCanonicalizer();
    private const string SharedKeyLiteAuthorizationScheme = "SharedKeyLite";
    private const int ExpectedCanonicalizedStringLength = 250;

    public static SharedKeyLiteCanonicalizer Instance
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

    private SharedKeyLiteCanonicalizer()
    {
        throw new System.NotImplementedException();
    }
}

}