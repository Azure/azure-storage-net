using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Text;
namespace Microsoft.WindowsAzure.Storage.Table
{
public sealed class SharedAccessTablePolicy
{
    public DateTimeOffset? SharedAccessStartTime
    {
        get; set;
    }

    public DateTimeOffset? SharedAccessExpiryTime
    {
        get; set;
    }

    public SharedAccessTablePermissions Permissions
    {
        get; set;
    }

    public static string PermissionsToString(SharedAccessTablePermissions permissions)
    {
        throw new System.NotImplementedException();
    }
    public static SharedAccessTablePermissions PermissionsFromString(string input)
    {
        throw new System.NotImplementedException();
    }
}

}