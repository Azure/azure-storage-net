using System;
namespace Microsoft.Azure.Storage
{
[Flags]
public enum SharedAccessAccountResourceTypes
{
    None = 0,
    Service = 1,
    Container = 2,
    Object = 4,
}

}