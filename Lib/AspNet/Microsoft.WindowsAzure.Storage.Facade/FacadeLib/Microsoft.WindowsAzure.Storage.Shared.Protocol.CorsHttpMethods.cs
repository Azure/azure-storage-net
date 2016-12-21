using System;
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
[Flags]
public enum CorsHttpMethods
{
    None = 0,
    Get = 1,
    Head = 2,
    Post = 4,
    Put = 8,
    Delete = 16,
    Trace = 32,
    Options = 64,
    Connect = 128,
    Merge = 256,
}

}