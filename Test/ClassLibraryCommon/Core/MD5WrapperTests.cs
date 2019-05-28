// -----------------------------------------------------------------------------------------
// <copyright file="MD5WrapperTests.cs" company="Microsoft">
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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;
using System.Security.Cryptography;

namespace Microsoft.Azure.Storage.Core.Util
{
    [TestClass]
    public class CRC64WrapperTests: TestBase
    {
        [TestMethod]
        public void UpdateHashTest()
        {
            var random = new Random();

            var minBatchSize = 1 * 1024;
            var maxBatchSize = 100 * 1024;

            var data = GetRandomBuffer(17 * 1024 * 1024);

            var wrapper = new Crc64Wrapper();
            var position = 0;

            do
            {
                var count = random.Next(minBatchSize, maxBatchSize);
                count = Math.Min(count, data.Length - position);
                var segment = new byte[count];

                Array.Copy(data, position, segment, 0, count);
                position += count;

                wrapper.UpdateHash(segment, 0, count);

            }
            while (position < data.Length);

            var crc0 = wrapper.ComputeHash();

            wrapper = new Crc64Wrapper();
            position = 0;

            do
            {
                var count = random.Next(minBatchSize, maxBatchSize);
                count = Math.Min(count, data.Length - position);
                var segment = new byte[count];

                Array.Copy(data, position, segment, 0, count);
                position += count;

                wrapper.UpdateHash(segment, 0, count);

            }
            while (position < data.Length);

            var crc1 = wrapper.ComputeHash();

            Assert.AreEqual(crc0, crc1);
        }
    }

    [TestClass()]
    public class MD5WrapperTests
    {
        private byte[] data = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8 };

        /// <summary>
        /// FISMA MD5 and nativemd5 comparison test
        /// </summary>
        [TestMethod()]
        public void MD5ComparisonTest()
        {
            CloudStorageAccount.UseV1MD5 = false;

            try
            {
                using (MD5Wrapper nativeHash = new MD5Wrapper())
                {
                    nativeHash.UpdateHash(data, 0, data.Length);
                    string nativeResult = nativeHash.ComputeHash();

                    MD5 hash = MD5.Create();
                    hash.TransformBlock(data, 0, data.Length, null, 0);
                    hash.TransformFinalBlock(new byte[0], 0, 0);
                    byte[] bytes = hash.Hash;
                    string result = Convert.ToBase64String(bytes);

                    Assert.AreEqual(nativeResult, result);
                }
            }
            finally
            {
                CloudStorageAccount.UseV1MD5 = true;
            }
        }
        
        /// <summary>
        /// Basic .net MD5 and nativemd5 comparison test
        /// </summary>
        [TestMethod()]
        public void MD5V1ComparisonTest()
        {
            using (MD5Wrapper nativeHash = new MD5Wrapper())
            {
                nativeHash.UpdateHash(data, 0, data.Length);
                string nativeResult = nativeHash.ComputeHash();

                MD5 hash = MD5.Create();
                hash.TransformBlock(data, 0, data.Length, null, 0);
                hash.TransformFinalBlock(new byte[0], 0, 0);
                byte[] bytes = hash.Hash;
                string result = Convert.ToBase64String(bytes);
                
                Assert.AreEqual(nativeResult, result);
            }
        }

        /// <summary>
        /// Test offset to the array
        /// </summary>
        [TestMethod()]
        public void MD5SingleByteTest()
        {
            CloudStorageAccount.UseV1MD5 = false;
            try
            {
                using (MD5Wrapper nativeHash = new MD5Wrapper())
                {
                    nativeHash.UpdateHash(data, 3, 2);
                    string nativeResult = nativeHash.ComputeHash();

                    MD5 hash = MD5.Create();
                    hash.TransformBlock(data, 3, 2, null, 0);
                    hash.TransformFinalBlock(new byte[0], 0, 0);
                    byte[] bytes = hash.Hash;
                    string result = Convert.ToBase64String(bytes);

                    Assert.AreEqual(nativeResult, result);
                }
            }
            finally
            {
                CloudStorageAccount.UseV1MD5 = true;
            }
        }

        [TestMethod]
        public void MD5EmptyArrayTest()
        {
            CloudStorageAccount.UseV1MD5 = false;
            byte[] data = new byte[] { };
            try
            {
                using (MD5Wrapper nativeHash = new MD5Wrapper())
                {
                    nativeHash.UpdateHash(data, 0, data.Length);
                    string nativeResult = nativeHash.ComputeHash();

                    MD5 hash = MD5.Create();
                    hash.ComputeHash(data, 0, data.Length);
                    byte[] varResult = hash.Hash;
                    string result = Convert.ToBase64String(varResult);

                    Assert.AreEqual(nativeResult, result);
                }
            }
            finally
            {
                CloudStorageAccount.UseV1MD5 = true;
            }
        }

        [TestMethod]
        public void MD5BigDataTest()
        {
            CloudStorageAccount.UseV1MD5 = false;
            byte[] data = new byte[10000];
            try
            {
                for (int i = 1; i < 10000; i++)
                {
                    data[i] = 1;
                }

                using (MD5Wrapper nativeHash = new MD5Wrapper())
                {
                    MD5 hash = MD5.Create();
                    for (int i = 0; i < 999; i++)
                    {
                        int index = 10 * i;
                        nativeHash.UpdateHash(data, 0, 10);
                        hash.TransformBlock(data, 0, 10, null, 0);
                    }
                    string nativeResult = nativeHash.ComputeHash();

                    hash.TransformFinalBlock(new byte[0], 0, 0);
                    byte[] varResult = hash.Hash;
                    String result = Convert.ToBase64String(varResult);

                    Assert.AreEqual(nativeResult, result);
                }
            }
            finally
            {
                CloudStorageAccount.UseV1MD5 = true;
            }
        }

        [TestMethod]
        public void MD5LastByteTest()
        {
            CloudStorageAccount.UseV1MD5 = false;
            try
            {
                using (MD5Wrapper nativeHash = new MD5Wrapper())
                {
                    nativeHash.UpdateHash(data, 8, 1);
                    string nativeResult = nativeHash.ComputeHash();


                    MD5 hash = MD5.Create();
                    hash.ComputeHash(data, 8, 1);
                    byte[] varResult = hash.Hash;
                    string result = Convert.ToBase64String(varResult);

                    Assert.AreEqual(nativeResult, result);
                }
            }
            finally
            {
                CloudStorageAccount.UseV1MD5 = true;
            }
        }
    }
}
