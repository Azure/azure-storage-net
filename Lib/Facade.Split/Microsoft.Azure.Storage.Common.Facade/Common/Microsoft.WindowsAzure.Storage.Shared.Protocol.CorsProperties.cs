using System.Collections.Generic;
namespace Microsoft.Azure.Storage.Shared.Protocol
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