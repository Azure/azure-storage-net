// -----------------------------------------------------------------------------------------
// <copyright file="TestHelper.cs" company="Microsoft">
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

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage
{
    /// <summary>
    /// Classes with test helper methods
    /// </summary>
    public partial class TestHelper
    {
        /// <summary>
        /// Compares the streams from the current position to the end.
        /// </summary>
        /// <param name="src">source stream for comparison</param>
        /// <param name="dst">destination stream for comparison</param>
        /// <returns></returns>
        internal static async Task AssertStreamsAreEqualAsync(Stream src, Stream dst)
        {
            Stream srcAsStream = src;
            Stream dstAsStream = dst;

            byte[] srcBuffer = new byte[64 * 1024];
            int srcRead;

            byte[] dstBuffer = new byte[64 * 1024];
            int dstRead;

            do
            {
                srcRead = await srcAsStream.ReadAsync(srcBuffer, 0, srcBuffer.Length);
                dstRead = await dstAsStream.ReadAsync(dstBuffer, 0, dstBuffer.Length);

                Assert.AreEqual(srcRead, dstRead);

                for (int i = 0; i < srcRead; i++)
                {
                    Assert.AreEqual(srcBuffer[i], dstBuffer[i]);
                }
            }
            while (srcRead > 0);
        }

        /// <summary>
        /// Runs a given operation that is expected to throw an exception.
        /// </summary>
        /// <typeparam name="T">Exeption type</typeparam>
        /// <param name="operation">operation to run</param>
        /// <param name="operationDescription">operation description</param>
        internal static async Task<T> ExpectedExceptionAsync<T>(Func<Task> operation, string operationDescription)
            where T : Exception
        {
            try
            {
                await operation();
            }
            catch (T e)
            {
                return e;
            }
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
        /// <param name="operation">operation to run</param>
        /// <param name="operationDescription">operation description</param>
        /// <param name="expectedStatusCode">Expected status code</param>
        internal static async Task ExpectedExceptionAsync(Func<Task> operation, OperationContext operationContext, string operationDescription, HttpStatusCode expectedStatusCode, string requestErrorCode = null)
        {
            try
            {
                await operation();
            }
            catch (Exception)
            {
                Assert.AreEqual((int)expectedStatusCode, operationContext.LastResult.HttpStatusCode, "Http status code is unexpected.");
                if (!string.IsNullOrEmpty(requestErrorCode))
                {
                    Assert.IsNotNull(operationContext.LastResult.ExtendedErrorInformation);
                    Assert.AreEqual(requestErrorCode, operationContext.LastResult.ExtendedErrorInformation.ErrorCode);
                }
                return;
            }

            Assert.Fail("No exception received while expecting {0}: {1}", expectedStatusCode, operationDescription);
        }
    }
}
