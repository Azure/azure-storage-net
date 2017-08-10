using System;
namespace Microsoft.WindowsAzure.Storage.Queue
{
[Flags]
public enum MessageUpdateFields
{
    Visibility = 1,
    Content = 2,
}

}