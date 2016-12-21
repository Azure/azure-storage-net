using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Text;
namespace Microsoft.WindowsAzure.Storage.Queue
{
public sealed class SharedAccessQueuePolicy
{
    public DateTimeOffset? SharedAccessStartTime
    {
        get; set;
    }

    public DateTimeOffset? SharedAccessExpiryTime
    {
        get; set;
    }

    public SharedAccessQueuePermissions Permissions
    {
        get; set;
    }

    public static string PermissionsToString(SharedAccessQueuePermissions permissions)
    {
        throw new System.NotImplementedException();
    }
    public static SharedAccessQueuePermissions PermissionsFromString(string input)
    {
        throw new System.NotImplementedException();
    }
}

}