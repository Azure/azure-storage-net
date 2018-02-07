using Microsoft.Azure.Storage.Core.Util;
using System;
using System.Text;
namespace Microsoft.Azure.Storage.File
{
public sealed class SharedAccessFilePolicy
{
    public DateTimeOffset? SharedAccessStartTime
    {
        get; set;
    }

    public DateTimeOffset? SharedAccessExpiryTime
    {
        get; set;
    }

    public SharedAccessFilePermissions Permissions
    {
        get; set;
    }

    public static string PermissionsToString(SharedAccessFilePermissions permissions)
    {
        throw new System.NotImplementedException();
    }
    public static SharedAccessFilePermissions PermissionsFromString(string input)
    {
        throw new System.NotImplementedException();
    }
}

}