using System;
namespace Microsoft.WindowsAzure.Storage
{
[Flags]
public enum SharedAccessAccountPermissions
{
    None = 0,
    Read = 1,
    Add = 2,
    Create = 4,
    Update = 8,
    ProcessMessages = 16,
    Write = 32,
    Delete = 64,
    List = 128,
}

}