// -----------------------------------------------------------------------------------------
// <copyright file="TestHelper.Common.cs" company="Microsoft">
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
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

#if WINDOWS_DESKTOP
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.File.Protocol;
#else
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using Microsoft.WindowsAzure.Storage.File.Protocol;
using Microsoft.WindowsAzure.Storage.File;
#endif

namespace Microsoft.WindowsAzure.Storage
{
    public partial class TestHelper
    {
        /// <summary>
        /// Runs a given operation that is expected to throw an exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="operationDescription"></param>
        internal static T ExpectedException<T>(Action operation, string operationDescription, string expectedMessage = null)
            where T : Exception
        {
            try
            {
                operation();
            }
            catch (T e)
            {
                if (!string.IsNullOrEmpty(expectedMessage))
                {
                    if (!expectedMessage.Equals(e.Message, StringComparison.Ordinal))
                    {
                        Assert.Fail("Incorrect message in exception.  Expected: {0}, actual: {1}", expectedMessage, e.Message);
                    }
                }
                return e;
            }
#if NETCORE
            catch (AggregateException ex)
            {
                ex = ex.Flatten();
                if (ex.InnerExceptions.Count > 1)
                {
                    Assert.Fail("Multiple exceptions {0} for operation: {1}", ex.GetType(), operationDescription);
                }

                T e = ex.InnerException as T; // Test framework changes the value under debugger
                if (e != null)
                {
                    return e;
                }
                Assert.Fail("Invalid exception {0} for operation: {1}", ex.GetType(), operationDescription);
            }
#endif
            catch (Exception ex)
            {
                T e = ex as T; // Test framework changes the value under debugger
                if (e != null)
                {
                    return e;
                }

                Assert.Fail("Invalid exception {0} for operation: {1}", ex.GetType(), operationDescription);
            }

            Assert.Fail("No exception received while expecting {0}: {1}", typeof(T).ToString(), operationDescription);
            return null;
        }

        /// <summary>
        /// Runs a given operation that is expected to throw an exception.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="operationDescription"></param>
        /// <param name="expectedStatusCode"></param>
        internal static void ExpectedException(Action<OperationContext> operation, string operationDescription, int expectedStatusCode)
        {
            OperationContext opContext = new OperationContext();
            try
            {
                operation(opContext);
            }
            catch (Exception)
            {
                Assert.AreEqual(expectedStatusCode, opContext.LastResult.HttpStatusCode, "Http status code is unexpected.");
                return;
            }

            Assert.Fail("No exception received while expecting {0}: {1}", expectedStatusCode, operationDescription);
        }


        internal static void AssertNAttempts(OperationContext ctx, int n)
        {
            Assert.AreEqual(n, ctx.RequestResults.Count(), String.Format("Operation took more than {0} attempt(s) to complete", n));
        }

#if TASK
        internal static void AssertCancellation(OperationContext ctx)
        {
            TestHelper.AssertNAttempts(ctx, 1);
            Assert.IsInstanceOfType(ctx.LastResult.Exception, typeof(StorageException));
            Assert.AreEqual("Operation was canceled by user.", ctx.LastResult.Exception.Message);
            Assert.AreEqual((int)HttpStatusCode.Unused, ((StorageException)ctx.LastResult.Exception).RequestInformation.HttpStatusCode);
            Assert.AreEqual("Unused", ((StorageException)ctx.LastResult.Exception).RequestInformation.HttpStatusMessage);
        }

        /// <summary>
        /// Runs a given operation that is expected to throw an exception.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="operationDescription"></param>
        internal static T ExpectedExceptionTask<T>(Task operation, string operationDescription)
            where T : Exception
        {
            try
            {
                operation.Wait();
            }
            catch (AggregateException ex)
            {
                ex = ex.Flatten();
                if (ex.InnerExceptions.Count > 1)
                {
                    Assert.Fail("Multiple exceptions {0} for operation: {1}", ex.GetType(), operationDescription);
                }

                T e = ex.InnerException as T; // Test framework changes the value under debugger
                if (e != null)
                {
                    Assert.IsTrue(operation.IsCompleted);
                    if (ex.InnerException is OperationCanceledException)
                    {
                        Assert.IsTrue(operation.IsCanceled);
                        Assert.IsFalse(operation.IsFaulted);
                        Assert.AreEqual(TaskStatus.Canceled, operation.Status);
                    }
                    else
                    {
                        Assert.IsFalse(operation.IsCanceled);
                        Assert.IsTrue(operation.IsFaulted);
                        Assert.AreEqual(TaskStatus.Faulted, operation.Status);
                    }

                    return e;
                }
                Assert.Fail("Invalid exception {0} for operation: {1}", ex.GetType(), operationDescription);
            }
            catch (Exception ex)
            {
                Assert.Fail("Invalid exception {0} for operation: {1}", ex.GetType(), operationDescription);
            }

            Assert.Fail("No exception received while expecting {0}: {1}", typeof(T).ToString(), operationDescription);
            return null;
        }
#endif

        /// <summary>
        /// Compares two streams.
        /// </summary>
        internal static void AssertStreamsAreEqual(Stream src, Stream dst)
        {
            Assert.AreEqual(src.Length, dst.Length);

            long origDstPosition = dst.Position;
            long origSrcPosition = src.Position;

            dst.Position = 0;
            src.Position = 0;

            for (int i = 0; i < src.Length; i++)
            {
                Assert.AreEqual(src.ReadByte(), dst.ReadByte());
            }

            dst.Position = origDstPosition;
            src.Position = origSrcPosition;
        }

        /// <summary>
        /// Compares two streams from the starting positions and up to length bytes.
        /// </summary>
        internal static void AssertStreamsAreEqualAtIndex(MemoryStream src, MemoryStream dst, int srcIndex, int dstIndex, int length)
        {
            byte[] origBuffer = src.ToArray();
            byte[] retrBuffer = dst.ToArray();

            for (int i = 0; i < length; i++)
            {
                Assert.AreEqual(origBuffer[srcIndex + i], retrBuffer[dstIndex + i]);
            }
        }

        /// <summary>
        /// Compares two byte buffers.
        /// </summary>
        internal static void AssertBuffersAreEqual(byte[] src, byte[] dst)
        {
            Assert.AreEqual(src.Length, dst.Length);

            for (int i = 0; i < src.Length; i++)
            {
                Assert.AreEqual(src[i], dst[i], "Mismatch of data at index : " + i);
            }
        }

        /// <summary>
        /// Compares two byte buffers.
        /// </summary>
        internal static void AssertBuffersAreEqualUptoIndex(byte[] src, byte[] dst, int index)
        {
            for (int i = 0; i <= index; i++)
            {
                Assert.AreEqual(src[i], dst[i], "Mismatch of data at index : " + i);
            }
        }

        /// <summary>
        /// Validates if this test supports the current target tenant. 
        /// Skips the current test if the target tenant is not supported. 
        /// </summary>
        public static void ValidateIfTestSupportTargetTenant(TenantType supportedTenantTypes)
        {
            if ((supportedTenantTypes & TestBase.CurrentTenantType) == 0)
            {
                Assert.Inconclusive("This test is skipped because the target test tenant is {0}.", TestBase.CurrentTenantType);
            }
        }

        /// <summary>
        /// Remove the local fiddler proxy from a URI.
        /// </summary>
        /// <param name="uri">The URI to change.</param>
        /// <returns>The URI without the local fiddler proxy.</returns>
        internal static Uri Defiddler(Uri uri)
        {
            string fiddlerString = "ipv4.fiddler";
            string replacementString = "127.0.0.1";

            string uriString = uri.AbsoluteUri;
            if (uriString.Contains(fiddlerString))
            {
                return new Uri(uriString.Replace(fiddlerString, replacementString));
            }
            else
            {
                return uri;
            }
        }

        /// <summary>
        /// Remove the local fiddler proxy from a blob reference.
        /// </summary>
        /// <param name="blob">The blob to change.</param>
        /// <returns>A blob reference without the local fiddler proxy.
        ///     If no fiddler proxy is present, the old blob reference is returned.</returns>
        internal static CloudBlockBlob Defiddler(CloudBlockBlob blob)
        {
            Uri oldUri = blob.Uri;
            Uri newUri = Defiddler(oldUri);

            if (newUri != oldUri)
            {
                return new CloudBlockBlob(newUri, blob.SnapshotTime, blob.ServiceClient.Credentials);
            }
            else
            {
                return blob;
            }
        }

        /// <summary>
        /// Remove the local fiddler proxy from a blob reference.
        /// </summary>
        /// <param name="blob">The blob to change.</param>
        /// <returns>A blob reference without the local fiddler proxy.
        ///     If no fiddler proxy is present, the old blob reference is returned.</returns>
        internal static CloudPageBlob Defiddler(CloudPageBlob blob)
        {
            Uri oldUri = blob.Uri;
            Uri newUri = Defiddler(oldUri);

            if (newUri != oldUri)
            {
                return new CloudPageBlob(newUri, blob.SnapshotTime, blob.ServiceClient.Credentials);
            }
            else
            {
                return blob;
            }
        }

        /// <summary>
        /// Remove the local fiddler proxy from a file reference.
        /// </summary>
        /// <param name="file">The file to change.</param>
        /// <returns>A file reference without the local fiddler proxy.
        ///     If no fiddler proxy is present, the old file reference is returned.</returns>
        internal static CloudFile Defiddler(CloudFile file)
        {
            Uri oldUri = file.Uri;
            Uri newUri = Defiddler(oldUri);

            if (newUri != oldUri)
            {
                return new CloudFile(newUri, file.ServiceClient.Credentials);
            }
            else
            {
                return file;
            }
        }

        internal static void ValidateResponse(OperationContext opContext, int expectedAttempts, int expectedStatusCode, string[] allowedErrorCodes, string errorMessageBeginsWith, string expectedStatusMessageBeginsWith = null)
        {
            ValidateResponse(opContext, expectedAttempts, expectedStatusCode, allowedErrorCodes, new string[] { errorMessageBeginsWith }, expectedStatusMessageBeginsWith);
        }

        internal static void ValidateResponse(OperationContext opContext, int expectedAttempts, int expectedStatusCode, string[] allowedErrorCodes, string[] errorMessageBeginsWith, string expectedStatusMessageBeginsWith = null)
        {
            ValidateResponse(opContext, expectedAttempts, expectedStatusCode, allowedErrorCodes, errorMessageBeginsWith, true, expectedStatusMessageBeginsWith);
        }

        internal static void ValidateResponse(OperationContext opContext, int expectedAttempts, int expectedStatusCode, string[] allowedErrorCodes, string[] errorMessageBeginsWith, bool stripIndex, string expectedStatusMessageBeginsWith = null)
        {
            TestHelper.AssertNAttempts(opContext, 1);
            Assert.AreEqual(opContext.LastResult.HttpStatusCode, expectedStatusCode);

            if (!string.IsNullOrEmpty(expectedStatusMessageBeginsWith))
            {
                Assert.IsTrue(opContext.LastResult.HttpStatusMessage.ToString().Contains(expectedStatusMessageBeginsWith));
            }

            Assert.IsTrue(allowedErrorCodes.Contains(opContext.LastResult.ExtendedErrorInformation.ErrorCode), "Unexpected Error Code, received " + opContext.LastResult.ExtendedErrorInformation.ErrorCode);

            if (errorMessageBeginsWith != null)
            {
                Assert.IsNotNull(opContext.LastResult.ExtendedErrorInformation.ErrorMessage);
                string message = opContext.LastResult.ExtendedErrorInformation.ErrorMessage;
                if (stripIndex)
                {
                    int semDex = opContext.LastResult.ExtendedErrorInformation.ErrorMessage.IndexOf(":");
                    semDex = semDex > 2 ? -1 : semDex;
                    message = message.Substring(semDex + 1);
                }

                Assert.IsTrue(errorMessageBeginsWith.Where((s) => message.StartsWith(s)).Count() > 0, opContext.LastResult.ExtendedErrorInformation.ErrorMessage);
            }
        }

        internal static void SeekRandomly(Stream stream, long offset)
        {
            Random random = new Random();
            int randomOrigin = random.Next(3);
            SeekOrigin origin = SeekOrigin.Begin;
            switch (randomOrigin)
            {
                case 1:
                    origin = SeekOrigin.Current;
                    offset = offset - stream.Position;
                    break;

                case 2:
                    origin = SeekOrigin.End;
                    offset = offset - stream.Length;
                    break;
            }
            stream.Seek(offset, origin);
        }

        internal static void VerifyServiceStats(ServiceStats stats)
        {
            Assert.IsNotNull(stats);
            if (stats.GeoReplication.LastSyncTime.HasValue)
            {
                Assert.AreEqual(GeoReplicationStatus.Live, stats.GeoReplication.Status);
            }
            else
            {
                Assert.AreNotEqual(GeoReplicationStatus.Live, stats.GeoReplication.Status);
            }
        }

        internal static void AssertFileServicePropertiesAreEqual(FileServiceProperties propsA, FileServiceProperties propsB)
        {
            if (propsA == null || propsB == null)
            {
                Assert.IsNull(propsA);
                Assert.IsNull(propsB);
            } else
            {
                AssertPropertiesAreEqual(propsA, propsA.serviceProperties);
                AssertPropertiesAreEqual(propsB, propsB.serviceProperties);
                AssertServicePropertiesAreEqual(propsA.serviceProperties, propsB.serviceProperties);
            }
        }

        private static void AssertPropertiesAreEqual(FileServiceProperties fileProps, ServiceProperties props)
        {
            if (fileProps == null || props == null)
            {
                Assert.IsNull(fileProps);
                Assert.IsNull(props);
            } else
            {
                Assert.AreEqual(fileProps.Cors, props.Cors);
                Assert.AreEqual(fileProps.MinuteMetrics, props.MinuteMetrics);
                Assert.AreEqual(fileProps.HourMetrics, props.HourMetrics);
            }
        }

        internal static void AssertServicePropertiesAreEqual(ServiceProperties propsA, ServiceProperties propsB)
        {
            if (propsA.Logging != null && propsB.Logging != null)
            {
                Assert.AreEqual(propsA.Logging.LoggingOperations, propsB.Logging.LoggingOperations);
                Assert.AreEqual(propsA.Logging.RetentionDays, propsB.Logging.RetentionDays);
                Assert.AreEqual(propsA.Logging.Version, propsB.Logging.Version);
            }
            else
            {
                Assert.IsNull(propsA.Logging);
                Assert.IsNull(propsA.Logging);
            }

            if (propsA.HourMetrics != null && propsB.HourMetrics != null)
            {
                Assert.AreEqual(propsA.HourMetrics.MetricsLevel, propsB.HourMetrics.MetricsLevel);
                Assert.AreEqual(propsA.HourMetrics.RetentionDays, propsB.HourMetrics.RetentionDays);
                Assert.AreEqual(propsA.HourMetrics.Version, propsB.HourMetrics.Version);
            }
            else
            {
                Assert.IsNull(propsA.HourMetrics);
                Assert.IsNull(propsB.HourMetrics);
            }

            if (propsA.MinuteMetrics != null && propsB.MinuteMetrics != null)
            {
                Assert.AreEqual(propsA.MinuteMetrics.MetricsLevel, propsB.MinuteMetrics.MetricsLevel);
                Assert.AreEqual(propsA.MinuteMetrics.RetentionDays, propsB.MinuteMetrics.RetentionDays);
                Assert.AreEqual(propsA.MinuteMetrics.Version, propsB.MinuteMetrics.Version);
            }
            else
            {
                Assert.IsNull(propsA.MinuteMetrics);
                Assert.IsNull(propsB.MinuteMetrics);
            }

            if (propsA.DefaultServiceVersion != null && propsB.DefaultServiceVersion != null)
            {
                Assert.AreEqual(propsA.DefaultServiceVersion, propsB.DefaultServiceVersion); 
            }
            else
            {
                Assert.IsNull(propsA.DefaultServiceVersion);
                Assert.IsNull(propsB.DefaultServiceVersion);
            }

            if (propsA.Cors != null && propsB.Cors != null)
            {
                Assert.AreEqual(propsA.Cors.CorsRules.Count, propsB.Cors.CorsRules.Count);

                // Check that rules are equal and in the same order.
                for (int i = 0; i < propsA.Cors.CorsRules.Count; i++)
                {
                    CorsRule ruleA = propsA.Cors.CorsRules.ElementAt(i);
                    CorsRule ruleB = propsB.Cors.CorsRules.ElementAt(i);

                    Assert.IsTrue(
                        ruleA.AllowedOrigins.Count == ruleB.AllowedOrigins.Count
                        && !ruleA.AllowedOrigins.Except(ruleB.AllowedOrigins).Any());

                    Assert.IsTrue(
                        ruleA.ExposedHeaders.Count == ruleB.ExposedHeaders.Count
                        && !ruleA.ExposedHeaders.Except(ruleB.ExposedHeaders).Any());

                    Assert.IsTrue(
                        ruleA.AllowedHeaders.Count == ruleB.AllowedHeaders.Count
                        && !ruleA.AllowedHeaders.Except(ruleB.AllowedHeaders).Any());

                    Assert.IsTrue(ruleA.AllowedMethods == ruleB.AllowedMethods);

                    Assert.IsTrue(ruleA.MaxAgeInSeconds == ruleB.MaxAgeInSeconds);
                } 
            }
            else
            {
                Assert.IsNull(propsA.Cors);
                Assert.IsNull(propsB.Cors);
            }
        }
    }
}
