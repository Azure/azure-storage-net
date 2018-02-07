
namespace Microsoft.Azure.Storage.Shared.Protocol
{
public sealed class LoggingProperties
{
    public string Version
    {
        get; set;
    }

    public LoggingOperations LoggingOperations
    {
        get; set;
    }

    public int? RetentionDays
    {
        get; set;
    }

    public LoggingProperties()
      : this("1.0")
    {
        throw new System.NotImplementedException();
    }
    public LoggingProperties(string version)
    {
        throw new System.NotImplementedException();
    }
}

}