// -----------------------------------------------------------------------------------------
// <copyright file="AnalyticsTestBase.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.WindowsAzure.Storage.Analytics
{
    public class AnalyticsTestBase : TestBase
    {
        public static string GetRandomContainerName()
        {
            return string.Concat("testc", Guid.NewGuid().ToString("N"));
        }

        public static string GenerateRandomTableName()
        {
            return "tbl" + Guid.NewGuid().ToString("N");
        }

#if SYNC
        public static List<string> CreateLogs(CloudBlobContainer container, StorageService service, int count, DateTime start, string granularity)
        {
            string name;
            List<string> blobs = new List<string>();

            for (int i = 0; i < count; i++)
            {
                CloudBlockBlob blockBlob;

                switch (granularity)
                {
                    case "hour":
                        name = string.Concat(service.ToString().ToLowerInvariant(), "/", start.AddHours(i).ToString("yyyy/MM/dd/HH", CultureInfo.InvariantCulture), "00/000001.log"); 
                        break;

                    case "day":
                        name = string.Concat(service.ToString().ToLowerInvariant(), "/", start.AddDays(i).ToString("yyyy/MM/dd/HH", CultureInfo.InvariantCulture), "00/000001.log"); 
                        break;

                    case "month":
                        name = string.Concat(service.ToString().ToLowerInvariant(), "/", start.AddMonths(i).ToString("yyyy/MM/dd/HH", CultureInfo.InvariantCulture), "00/000001.log"); 
                        break;

                    default:
                        throw new ArgumentException(string.Format(CultureInfo.CurrentCulture, "CreateLogs granularity of '{0}' is invalid.", granularity));
                }

                blockBlob = container.GetBlockBlobReference(name);
                blockBlob.PutBlockList(new string[] { });
                blobs.Add(name);
            }

            return blobs;
        }
#endif
    }
}
