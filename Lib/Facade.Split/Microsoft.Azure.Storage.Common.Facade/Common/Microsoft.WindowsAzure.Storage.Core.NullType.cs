
namespace Microsoft.WindowsAzure.Storage.Core
{
public sealed class NullType
{
    internal static readonly NullType Value = new NullType();

    private NullType()
    {
        throw new System.NotImplementedException();
    }
}

}