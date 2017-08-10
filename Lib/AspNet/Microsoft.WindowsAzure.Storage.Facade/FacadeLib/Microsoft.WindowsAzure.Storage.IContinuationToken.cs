
namespace Microsoft.WindowsAzure.Storage
{
public interface IContinuationToken
{
    StorageLocation? TargetLocation
    {
        get; set;
    }
}

}