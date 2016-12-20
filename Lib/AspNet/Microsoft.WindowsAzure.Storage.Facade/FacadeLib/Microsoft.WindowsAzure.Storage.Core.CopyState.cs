using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
namespace Microsoft.WindowsAzure.Storage.Core
{

internal class CopyState
{
    public Stream Destination
    {
        get; set;
    }

    public DateTime? ExpiryTime
    {
        get; set;
    }
}

}