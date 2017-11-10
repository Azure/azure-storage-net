 
namespace Microsoft.Azure.Storage.Core.Auth
{
internal interface ICanonicalizer
{
    string AuthorizationScheme
    {
        get;
    }
}

}