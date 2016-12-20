using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
public sealed class CorsProperties
{
    public IList<CorsRule> CorsRules
    {
        get; internal set;
    }

    public CorsProperties()
    {
        throw new System.NotImplementedException();
    }
}

}