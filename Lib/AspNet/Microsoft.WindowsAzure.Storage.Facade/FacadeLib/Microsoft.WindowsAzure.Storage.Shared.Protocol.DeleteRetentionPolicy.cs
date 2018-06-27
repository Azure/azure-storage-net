
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
public sealed class DeleteRetentionPolicy
{
    public bool Enabled
    {
        get; set;
    }

    public int? RetentionDays
    {
        get; set;
    }
}

}