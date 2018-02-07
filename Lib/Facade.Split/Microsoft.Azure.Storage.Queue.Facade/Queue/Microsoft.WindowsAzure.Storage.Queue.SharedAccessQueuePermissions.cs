using System;
namespace Microsoft.Azure.Storage.Queue
{
[Flags]
public enum SharedAccessQueuePermissions
{
    None = 0,
    Read = 1,
    Add = 2,
    Update = 4,
    ProcessMessages = 8,
}

}