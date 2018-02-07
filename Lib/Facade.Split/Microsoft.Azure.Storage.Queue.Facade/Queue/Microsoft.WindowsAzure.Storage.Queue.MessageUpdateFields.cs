using System;
namespace Microsoft.Azure.Storage.Queue
{
[Flags]
public enum MessageUpdateFields
{
    Visibility = 1,
    Content = 2,
}

}