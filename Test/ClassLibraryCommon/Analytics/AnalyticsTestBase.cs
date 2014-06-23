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

namespace Microsoft.WindowsAzure.Storage.Analytics
{
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
#if WINDOWS_DESKTOP
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

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

        public static void AssertLogItemsEqual(LogRecord expected, LogRecord actual)
        {
            Assert.AreEqual(expected.VersionNumber, actual.VersionNumber);

            Assert.AreEqual(expected.RequestStartTime, actual.RequestStartTime);
            Assert.AreEqual(expected.OperationType, actual.OperationType);
            Assert.AreEqual(expected.RequestStatus, actual.RequestStatus);
            Assert.AreEqual(expected.HttpStatusCode, actual.HttpStatusCode);
            Assert.AreEqual(expected.EndToEndLatency, actual.EndToEndLatency);
            Assert.AreEqual(expected.ServerLatency, actual.ServerLatency);
            Assert.AreEqual(expected.AuthenticationType, actual.AuthenticationType);
            Assert.AreEqual(expected.RequesterAccountName, actual.RequesterAccountName);
            Assert.AreEqual(expected.OwnerAccountName, actual.OwnerAccountName);
            Assert.AreEqual(expected.ServiceType, actual.ServiceType);
            Assert.AreEqual(expected.RequestUrl, actual.RequestUrl);
            Assert.AreEqual(expected.RequestedObjectKey, actual.RequestedObjectKey);
            Assert.AreEqual(expected.RequestIdHeader, actual.RequestIdHeader);
            Assert.AreEqual(expected.OperationCount, actual.OperationCount);
            Assert.AreEqual(expected.RequesterIPAddress, actual.RequesterIPAddress);
            Assert.AreEqual(expected.RequestVersionHeader, actual.RequestVersionHeader);
            Assert.AreEqual(expected.RequestHeaderSize, actual.RequestHeaderSize);
            Assert.AreEqual(expected.RequestPacketSize, actual.RequestPacketSize);
            Assert.AreEqual(expected.ResponseHeaderSize, actual.ResponseHeaderSize);
            Assert.AreEqual(expected.ResponsePacketSize, actual.ResponsePacketSize);
            Assert.AreEqual(expected.RequestContentLength, actual.RequestContentLength);
            Assert.AreEqual(expected.RequestMD5, actual.RequestMD5);
            Assert.AreEqual(expected.ServerMD5, actual.ServerMD5);
            Assert.AreEqual(expected.ETagIdentifier, actual.ETagIdentifier);
            Assert.AreEqual(expected.LastModifiedTime, actual.LastModifiedTime);
            Assert.AreEqual(expected.ConditionsUsed, actual.ConditionsUsed);
            Assert.AreEqual(expected.UserAgentHeader, actual.UserAgentHeader);
            Assert.AreEqual(expected.ReferrerHeader, actual.ReferrerHeader);
            Assert.AreEqual(expected.ClientRequestId, actual.ClientRequestId);
        }
    }
}
