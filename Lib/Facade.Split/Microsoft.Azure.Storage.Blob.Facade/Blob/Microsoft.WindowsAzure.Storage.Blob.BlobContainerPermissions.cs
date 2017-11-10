
namespace Microsoft.Azure.Storage.Blob
{
public sealed class BlobContainerPermissions
{
    public BlobContainerPublicAccessType PublicAccess
    {
        get; set;
    }

    public SharedAccessBlobPolicies SharedAccessPolicies
    {
        get; private set;
    }

    public BlobContainerPermissions()
    {
        throw new System.NotImplementedException();
    }
}

}