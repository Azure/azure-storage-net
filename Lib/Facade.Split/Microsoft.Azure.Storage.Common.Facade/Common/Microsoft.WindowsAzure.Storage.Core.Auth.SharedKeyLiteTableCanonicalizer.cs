using Microsoft.Azure.Storage.Core.Util;
 
namespace Microsoft.Azure.Storage.Core.Auth
{
internal sealed class SharedKeyLiteTableCanonicalizer : ICanonicalizer
{
    private static SharedKeyLiteTableCanonicalizer instance = new SharedKeyLiteTableCanonicalizer();
    private const string SharedKeyLiteAuthorizationScheme = "SharedKeyLite";
    private const int ExpectedCanonicalizedStringLength = 150;

    public static SharedKeyLiteTableCanonicalizer Instance
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

    private SharedKeyLiteTableCanonicalizer()
    {
        throw new System.NotImplementedException();
    }
}

}