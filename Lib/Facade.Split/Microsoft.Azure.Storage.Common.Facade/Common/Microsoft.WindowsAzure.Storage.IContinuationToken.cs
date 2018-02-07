
namespace Microsoft.Azure.Storage
{
public interface IContinuationToken
{
    StorageLocation? TargetLocation
    {
        get; set;
    }
}

}