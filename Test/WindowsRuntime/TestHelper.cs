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
using Windows.Storage.Streams;

namespace Microsoft.Azure.Storage
{
    public partial class TestHelper
    {

        /// <summary>
        /// Compares the streams from the current position to the end.
        /// </summary>
        internal static async Task AssertStreamsAreEqualAsync(IInputStream src, IInputStream dst)
        {
            Stream srcAsStream = src.AsStreamForRead();
            Stream dstAsStream = dst.AsStreamForRead();

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
    }
}
