 
namespace Microsoft.Azure.Storage.Core.Auth
{
public interface ICanonicalizer
{
    string AuthorizationScheme
    {
        get;
    }
}

}