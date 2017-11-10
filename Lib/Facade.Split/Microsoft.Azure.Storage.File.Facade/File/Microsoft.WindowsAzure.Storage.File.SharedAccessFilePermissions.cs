using System;
namespace Microsoft.Azure.Storage.File
{
[Flags]
public enum SharedAccessFilePermissions
{
    None = 0,
    Read = 1,
    Write = 2,
    Delete = 4,
    List = 8,
    Create = 16,
}

}