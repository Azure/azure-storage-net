using System;
namespace Microsoft.Azure.Storage.Shared.Protocol
{
[Flags]
public enum LoggingOperations
{
    None = 0,
    Read = 1,
    Write = 2,
    Delete = 4,
    All = Delete | Write | Read,
}

}