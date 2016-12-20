using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Text;
namespace Microsoft.WindowsAzure.Storage.Blob
{
public sealed class SharedAccessBlobPolicy
{
    public DateTimeOffset? SharedAccessStartTime
    {
        get; set;
    }

    public DateTimeOffset? SharedAccessExpiryTime
    {
        get; set;
    }

    public SharedAccessBlobPermissions Permissions
    {
        get; set;
    }

    public static string PermissionsToString(SharedAccessBlobPermissions permissions)
    {
        throw new System.NotImplementedException();
    }
    public static SharedAccessBlobPermissions PermissionsFromString(string input)
    {
        throw new System.NotImplementedException();
    }
}

}