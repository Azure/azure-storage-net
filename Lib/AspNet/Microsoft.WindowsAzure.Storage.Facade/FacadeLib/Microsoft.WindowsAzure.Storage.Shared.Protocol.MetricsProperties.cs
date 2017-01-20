
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
public sealed class MetricsProperties
{
    public string Version
    {
        get; set;
    }

    public MetricsLevel MetricsLevel
    {
        get; set;
    }

    public int? RetentionDays
    {
        get; set;
    }

    public MetricsProperties()
      : this("1.0")
    {
        throw new System.NotImplementedException();
    }
    public MetricsProperties(string version)
    {
        throw new System.NotImplementedException();
    }
}

}