using System;
using System.Text;
namespace Microsoft.WindowsAzure.Storage
{
public sealed class SharedAccessAccountPolicy
{
    public DateTimeOffset? SharedAccessStartTime
    {
        get; set;
    }

    public DateTimeOffset? SharedAccessExpiryTime
    {
        get; set;
    }

    public SharedAccessAccountPermissions Permissions
    {
        get; set;
    }

    public SharedAccessAccountServices Services
    {
        get; set;
    }

    public SharedAccessAccountResourceTypes ResourceTypes
    {
        get; set;
    }

    public SharedAccessProtocol? Protocols
    {
        get; set;
    }

    public IPAddressOrRange IPAddressOrRange
    {
        get; set;
    }

    public static string PermissionsToString(SharedAccessAccountPermissions permissions)
    {
        throw new System.NotImplementedException();
    }
    public static string ServicesToString(SharedAccessAccountServices services)
    {
        throw new System.NotImplementedException();
    }
    public static string ResourceTypesToString(SharedAccessAccountResourceTypes resourceTypes)
    {
        throw new System.NotImplementedException();
    }
}

}