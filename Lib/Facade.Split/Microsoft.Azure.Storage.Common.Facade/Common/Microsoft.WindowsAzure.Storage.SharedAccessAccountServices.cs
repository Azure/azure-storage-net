using System;
namespace Microsoft.Azure.Storage
{
[Flags]
public enum SharedAccessAccountServices
{
    None = 0,
    Blob = 1,
    File = 2,
    Queue = 4,
    Table = 8,
}

}