using Microsoft.WindowsAzure.Storage.Core;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage
{
public static class ODataErrorHelper
{

    public static StorageExtendedErrorInformation ReadAndParseExtendedError(Stream inputStream)
    {
        throw new System.NotImplementedException();
    }
    public static Task<StorageExtendedErrorInformation> ReadAndParseExtendedErrorAsync(Stream responseStream, CancellationToken cancellationToken)
    {
        throw new System.NotImplementedException();
    }
}

}