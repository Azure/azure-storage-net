// -----------------------------------------------------------------------------------------
// <copyright file="BlobClientEncryptionTests.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Blob
{
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Security.Cryptography;
    using System.Threading;
    using System.Threading.Tasks;

    [TestClass]
    public class BlobClientEncryptionTests : BlobTestBase
    {
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.BlobBufferManager != null)
            {
                TestBase.BlobBufferManager.OutstandingBufferCount = 0;
            }
        }

        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.BlobBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.BlobBufferManager.OutstandingBufferCount);
            }
        }

        [TestMethod]
        [Description("Upload and download encrypted blob.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobBasicEncryption()
        {
            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(() => this.DoCloudBlobEncryption(BlobType.BlockBlob, false)));
            tasks.Add(Task.Run(() => this.DoCloudBlobEncryption(BlobType.PageBlob, false)));
            tasks.Add(Task.Run(() => this.DoCloudBlobEncryption(BlobType.AppendBlob, false)));

            tasks.Add(Task.Run(() => this.DoCloudBlobEncryption(BlobType.BlockBlob, true)));
            tasks.Add(Task.Run(() => this.DoCloudBlobEncryption(BlobType.PageBlob, true)));
            tasks.Add(Task.Run(() => this.DoCloudBlobEncryption(BlobType.AppendBlob, true)));
            Task.WaitAll(tasks.ToArray());
        }

        private void DoCloudBlobEncryption(BlobType type, bool partial)
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                int size = 5 * 1024 * 1024;
                byte[] buffer = GetRandomBuffer(size);

                if (partial)
                {
                    size = 2 * 1024 * 1024;
                }

                ICloudBlob blob;
                if (type == BlobType.BlockBlob)
                {
                    blob = container.GetBlockBlobReference("blockblob");
                }
                else if (type == BlobType.PageBlob)
                {
                    blob = container.GetPageBlobReference("pageblob");
                }
                else
                {
                    blob = container.GetAppendBlobReference("appendblob");
                }

                #region sample_RequestOptions_EncryptionPolicy

                // Create the Key to be used for wrapping.
                // This code creates a random encryption key.
                SymmetricKey aesKey = new SymmetricKey(kid: "symencryptionkey");

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(key: aesKey, keyResolver: null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                // Encrypt and upload the data to the blob.
                MemoryStream stream = new MemoryStream(buffer);
                blob.UploadFromStream(stream, length: size, accessCondition: null, options: uploadOptions);
                
                #endregion

                // Ensure that the user stream is open.
                Assert.IsTrue(stream.CanSeek);
                stream.Dispose();

                // Create the resolver to be used for unwrapping.
                DictionaryKeyResolver resolver = new DictionaryKeyResolver();
                resolver.Add(aesKey);

                // Download the encrypted blob.
                // Create the decryption policy to be used for download. There is no need to specify the
                // key when the policy is only going to be used for downloads. Resolver is sufficient.
                BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, resolver);

                // Set the decryption policy on the request options.
                BlobRequestOptions downloadOptions = new BlobRequestOptions() { EncryptionPolicy = downloadPolicy };

                // Download and decrypt the encrypted contents from the blob.
                MemoryStream outputStream = new MemoryStream();
                blob.DownloadToStream(outputStream, null, downloadOptions, null);

                // Ensure that the user stream is open.
                outputStream.Seek(0, SeekOrigin.Begin);

                // Compare that the decrypted contents match the input data.
                byte[] outputArray = outputStream.ToArray();
                TestHelper.AssertBuffersAreEqualUptoIndex(outputArray, buffer, size - 1);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload and download encrypted blob.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobBasicEncryptionAPM()
        {
            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(() => DoCloudBlobEncryptionAPM(BlobType.BlockBlob, false)));
            tasks.Add(Task.Run(() => DoCloudBlobEncryptionAPM(BlobType.PageBlob, false)));
            tasks.Add(Task.Run(() => DoCloudBlobEncryptionAPM(BlobType.AppendBlob, false)));

            tasks.Add(Task.Run(() => DoCloudBlobEncryptionAPM(BlobType.BlockBlob, true)));
            tasks.Add(Task.Run(() => DoCloudBlobEncryptionAPM(BlobType.PageBlob, true)));
            tasks.Add(Task.Run(() => DoCloudBlobEncryptionAPM(BlobType.AppendBlob, true)));
            Task.WaitAll(tasks.ToArray());
        }

        private static void DoCloudBlobEncryptionAPM(BlobType type, bool partial)
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                int size = 5 * 1024 * 1024;
                byte[] buffer = GetRandomBuffer(size);

                if (partial)
                {
                    size = 2 * 1024 * 1024;
                }

                ICloudBlob blob = GetCloudBlobReference(type, container);

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the resolver to be used for unwrapping.
                DictionaryKeyResolver resolver = new DictionaryKeyResolver();
                resolver.Add(aesKey);

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                MemoryStream stream;
                // Upload the encrypted contents to the blob.
                using (stream = new MemoryStream(buffer))
                {
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        ICancellableAsyncResult result = blob.BeginUploadFromStream(
                                            stream, size, null, uploadOptions, null, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blob.EndUploadFromStream(result);
                    }

                    // Ensure that the user stream is open.
                    Assert.IsTrue(stream.CanSeek);
                }

                // Download the encrypted blob.
                // Create the decryption policy to be used for download. There is no need to specify the encryption mode 
                // and the key wrapper when the policy is only going to be used for downloads.
                BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, resolver);

                // Set the decryption policy on the request options.
                BlobRequestOptions downloadOptions = new BlobRequestOptions() { EncryptionPolicy = downloadPolicy };

                // Download and decrypt the encrypted contents from the blob.
                MemoryStream outputStream = new MemoryStream();
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    ICancellableAsyncResult result = blob.BeginDownloadToStream(outputStream, null, downloadOptions, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndDownloadToStream(result);
                }

                // Ensure that the user stream is open.
                outputStream.Seek(0, SeekOrigin.Begin);

                // Compare that the decrypted contents match the input data.
                byte[] outputArray = outputStream.ToArray();
                TestHelper.AssertBuffersAreEqualUptoIndex(outputArray, buffer, size - 1);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload and download encrypted blob from/to a file.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobEncryptionWithFile()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                int size = 5 * 1024 * 1024;
                byte[] buffer = GetRandomBuffer(size);

                CloudBlockBlob blob = container.GetBlockBlobReference("blockblob");

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the resolver to be used for unwrapping.
                DictionaryKeyResolver resolver = new DictionaryKeyResolver();
                resolver.Add(aesKey);

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                string inputFileName = Path.GetTempFileName();
                string outputFileName = Path.GetTempFileName();

                using (FileStream file = new FileStream(inputFileName, FileMode.Create, FileAccess.Write))
                {
                    file.Write(buffer, 0, buffer.Length);
                }

                // Upload the encrypted contents to the blob.
                blob.UploadFromFile(inputFileName, null, uploadOptions, null);

                // Download the encrypted blob.
                // Create the decryption policy to be used for download. There is no need to specify the
                // key when the policy is only going to be used for downloads. Resolver is sufficient.
                BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, resolver);

                // Set the decryption policy on the request options.
                BlobRequestOptions downloadOptions = new BlobRequestOptions() { EncryptionPolicy = downloadPolicy };

                // Download and decrypt the encrypted contents from the blob.
                blob.DownloadToFile(outputFileName, FileMode.Create, null, downloadOptions, null);

                // Compare that the decrypted contents match the input data.
                using (FileStream inputFileStream = new FileStream(inputFileName, FileMode.Open, FileAccess.Read),
                        outputFileStream = new FileStream(outputFileName, FileMode.Open, FileAccess.Read))
                {
                    TestHelper.AssertStreamsAreEqual(inputFileStream, outputFileStream);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload and download encrypted blob from/to a byte array.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobEncryptionWithByteArray()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                int size = 5 * 1024 * 1024;
                byte[] buffer = GetRandomBuffer(size);
                byte[] outputBuffer = new byte[size];

                CloudBlockBlob blob = container.GetBlockBlobReference("blockblob");

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the resolver to be used for unwrapping.
                DictionaryKeyResolver resolver = new DictionaryKeyResolver();
                resolver.Add(aesKey);

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                // Upload the encrypted contents to the blob.
                blob.UploadFromByteArray(buffer, 0, buffer.Length, null, uploadOptions, null);

                // Download the encrypted blob.
                // Create the decryption policy to be used for download. There is no need to specify the
                // key when the policy is only going to be used for downloads. Resolver is sufficient.
                BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, resolver);

                // Set the decryption policy on the request options.
                BlobRequestOptions downloadOptions = new BlobRequestOptions() { EncryptionPolicy = downloadPolicy };

                // Download and decrypt the encrypted contents from the blob.
                blob.DownloadToByteArray(outputBuffer, 0, null, downloadOptions, null);

                // Compare that the decrypted contents match the input data.
                TestHelper.AssertBuffersAreEqual(buffer, outputBuffer);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Upload and download encrypted blob from/to text.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobEncryptionWithText()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                string data = "String data";

                CloudBlockBlob blob = container.GetBlockBlobReference("blockblob");

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the resolver to be used for unwrapping.
                DictionaryKeyResolver resolver = new DictionaryKeyResolver();
                resolver.Add(aesKey);

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                // Upload the encrypted contents to the blob.
                blob.UploadText(data, null, null, uploadOptions, null);

                // Download the encrypted blob.
                // Create the decryption policy to be used for download. There is no need to specify the
                // key when the policy is only going to be used for downloads. Resolver is sufficient.
                BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, resolver);

                // Set the decryption policy on the request options.
                BlobRequestOptions downloadOptions = new BlobRequestOptions() { EncryptionPolicy = downloadPolicy };

                // Download and decrypt the encrypted contents from the blob.
                string outputData = blob.DownloadText(null, null, downloadOptions, null);

                // Compare that the decrypted contents match the input data.
                Assert.AreEqual(data, outputData);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate AES and RSA key wrappers.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobEncryptionValidateWrappers()
        {
            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");
            RsaKey rsaKey = new RsaKey("asymencryptionkey");

            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);
            resolver.Add(rsaKey);

            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(() => DoCloudBlockBlobEncryptionValidateWrappers(aesKey, resolver)));
            tasks.Add(Task.Run(() => DoCloudBlockBlobEncryptionValidateWrappers(rsaKey, resolver)));
            Task.WaitAll(tasks.ToArray());
        }

        private static void DoCloudBlockBlobEncryptionValidateWrappers(IKey key, DictionaryKeyResolver keyResolver)
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                int size = 5 * 1024 * 1024;
                byte[] buffer = GetRandomBuffer(size);

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(key, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                MemoryStream stream;
                // Upload the encrypted contents to the blob.
                using (stream = new MemoryStream(buffer))
                {
                    blob.UploadFromStream(stream, size, null, uploadOptions, null);

                    // Ensure that the user stream is open.
                    Assert.IsTrue(stream.CanSeek);
                }

                // Download the encrypted blob.
                // Create the decryption policy to be used for download. There is no need to specify the encryption mode 
                // and the key wrapper when the policy is only going to be used for downloads.
                BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, keyResolver);

                // Set the decryption policy on the request options.
                BlobRequestOptions downloadOptions = new BlobRequestOptions() { EncryptionPolicy = downloadPolicy };

                // Download and decrypt the encrypted contents from the blob.
                MemoryStream outputStream = new MemoryStream();
                blob.DownloadToStream(outputStream, null, downloadOptions, null);

                // Ensure that the user stream is open.
                outputStream.Seek(0, SeekOrigin.Begin);

                // Compare that the decrypted contents match the input data.
                byte[] outputArray = outputStream.ToArray();
                TestHelper.AssertBuffersAreEqual(outputArray, buffer);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate encryption.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobValidateEncryption()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                int size = 5 * 1024 * 1024;
                byte[] buffer = GetRandomBuffer(size);

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                // Upload the encrypted contents to the blob.
                MemoryStream stream = new MemoryStream(buffer);
                blob.UploadFromStream(stream, size, null, uploadOptions, null);

                // Encrypt locally.
                CryptoStream encryptedStream;
                using (AesCryptoServiceProvider myAes = new AesCryptoServiceProvider())
                {
                    string metadata = blob.Metadata[Constants.EncryptionConstants.BlobEncryptionData];
                    BlobEncryptionData encryptionData = JsonConvert.DeserializeObject<BlobEncryptionData>(metadata);
                    myAes.IV = encryptionData.ContentEncryptionIV;
                    myAes.Key = aesKey.UnwrapKeyAsync(encryptionData.WrappedContentKey.EncryptedKey, encryptionData.WrappedContentKey.Algorithm, CancellationToken.None).Result;

                    stream.Seek(0, SeekOrigin.Begin);
                    encryptedStream = new CryptoStream(stream, myAes.CreateEncryptor(), CryptoStreamMode.Read);
                }

                // Download the encrypted blob.
                MemoryStream outputStream = new MemoryStream();
                blob.DownloadToStream(outputStream);

                outputStream.Seek(0, SeekOrigin.Begin);
                for (int i = 0; i < outputStream.Length; i++)
                {
                    Assert.AreEqual(encryptedStream.ReadByte(), outputStream.ReadByte());
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate encryption.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobValidateEncryptionAPM()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                int size = 5 * 1024 * 1024;
                byte[] buffer = GetRandomBuffer(size);

                CloudBlockBlob blob = container.GetBlockBlobReference("blob1");

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                // Upload the encrypted contents to the blob.
                MemoryStream stream = new MemoryStream(buffer);
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    ICancellableAsyncResult result = blob.BeginUploadFromStream(
                                        stream, size, null, uploadOptions, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndUploadFromStream(result);
                }

                // Encrypt locally.
                CryptoStream encryptedStream;
                using (AesCryptoServiceProvider myAes = new AesCryptoServiceProvider())
                {
                    string metadata = blob.Metadata[Constants.EncryptionConstants.BlobEncryptionData];
                    BlobEncryptionData encryptionData = JsonConvert.DeserializeObject<BlobEncryptionData>(metadata);
                    myAes.IV = encryptionData.ContentEncryptionIV;
                    myAes.Key = aesKey.UnwrapKeyAsync(encryptionData.WrappedContentKey.EncryptedKey, encryptionData.WrappedContentKey.Algorithm, CancellationToken.None).Result;

                    stream.Seek(0, SeekOrigin.Begin);
                    encryptedStream = new CryptoStream(stream, myAes.CreateEncryptor(), CryptoStreamMode.Read);
                }

                // Download the encrypted blob.
                MemoryStream outputStream = new MemoryStream();

                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    ICancellableAsyncResult result = blob.BeginDownloadToStream(outputStream, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndDownloadToStream(result);
                }

                outputStream.Seek(0, SeekOrigin.Begin);
                for (int i = 0; i < outputStream.Length; i++)
                {
                    Assert.AreEqual(encryptedStream.ReadByte(), outputStream.ReadByte());
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate range download of encrypted blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlockBlobEncryptionValidateRangeDecryption()
        {
            this.ValidateRangeDecryption(BlobType.BlockBlob, 2 * 512, 1 * 512, 1 * 512);
            this.ValidateRangeDecryption(BlobType.BlockBlob, 2 * 512, null, null);
            this.ValidateRangeDecryption(BlobType.BlockBlob, 2 * 512, 1 * 512, null);
            this.ValidateRangeDecryption(BlobType.BlockBlob, 2 * 512, 0, 1 * 512);
            this.ValidateRangeDecryption(BlobType.BlockBlob, 2 * 512, 4, 1 * 512);
            this.ValidateRangeDecryption(BlobType.BlockBlob, 1325, 368, 495);
            this.ValidateRangeDecryption(BlobType.BlockBlob, 1325, 369, 495);

            // Edge cases
            this.ValidateRangeDecryption(BlobType.BlockBlob, 1024, 1023, 1);
            this.ValidateRangeDecryption(BlobType.BlockBlob, 1024, 0, 1);
            this.ValidateRangeDecryption(BlobType.BlockBlob, 1024, 512, 1);
            this.ValidateRangeDecryption(BlobType.BlockBlob, 1024, 0, 512);

            // Check cases outside the blob size but within the padded size
            this.ValidateRangeDecryption(BlobType.BlockBlob, 1025, 1023, 4, 2);
            this.ValidateRangeDecryption(BlobType.BlockBlob, 1025, 1023, 16, 2);
            this.ValidateRangeDecryption(BlobType.BlockBlob, 1025, 1023, 17, 2);
            this.ValidateRangeDecryption(BlobType.BlockBlob, 1025, 1024, 16, 1);
        }

        [TestMethod]
        [Description("Validate range download of encrypted blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudPageBlobEncryptionValidateRangeDecryption()
        {
            this.ValidateRangeDecryption(BlobType.PageBlob, 2 * 512, 1 * 512, 1 * 512 - 16);
            this.ValidateRangeDecryption(BlobType.PageBlob, 2 * 512, null, null);
            this.ValidateRangeDecryption(BlobType.PageBlob, 2 * 512, 1 * 512, null);
            this.ValidateRangeDecryption(BlobType.PageBlob, 2 * 512, 0, 1 * 512);
            this.ValidateRangeDecryption(BlobType.PageBlob, 2 * 512, 4, 1 * 512);

            // Edge cases
            this.ValidateRangeDecryption(BlobType.PageBlob, 1024, 1023, 1);
            this.ValidateRangeDecryption(BlobType.PageBlob, 1024, 0, 1);
            this.ValidateRangeDecryption(BlobType.PageBlob, 1024, 512, 1);
            this.ValidateRangeDecryption(BlobType.PageBlob, 1024, 0, 512);
        }

        [TestMethod]
        [Description("Validate range download of encrypted blob")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudAppendBlobEncryptionValidateRangeDecryption()
        {
            this.ValidateRangeDecryption(BlobType.AppendBlob, 2 * 512, 1 * 512, 1 * 512);
            this.ValidateRangeDecryption(BlobType.AppendBlob, 2 * 512, null, null);
            this.ValidateRangeDecryption(BlobType.AppendBlob, 2 * 512, 1 * 512, null);
            this.ValidateRangeDecryption(BlobType.AppendBlob, 2 * 512, 0, 1 * 512);
            this.ValidateRangeDecryption(BlobType.AppendBlob, 2 * 512, 4, 1 * 512);
            this.ValidateRangeDecryption(BlobType.AppendBlob, 1325, 368, 495);
            this.ValidateRangeDecryption(BlobType.AppendBlob, 1325, 369, 495);

            // Edge cases
            this.ValidateRangeDecryption(BlobType.AppendBlob, 1024, 1023, 1);
            this.ValidateRangeDecryption(BlobType.AppendBlob, 1024, 0, 1);
            this.ValidateRangeDecryption(BlobType.AppendBlob, 1024, 512, 1);
            this.ValidateRangeDecryption(BlobType.AppendBlob, 1024, 0, 512);

            // Check cases outside the blob size but within the padded size
            this.ValidateRangeDecryption(BlobType.AppendBlob, 1025, 1023, 4, 2);
            this.ValidateRangeDecryption(BlobType.AppendBlob, 1025, 1023, 16, 2);
            this.ValidateRangeDecryption(BlobType.AppendBlob, 1025, 1023, 17, 2);
            this.ValidateRangeDecryption(BlobType.AppendBlob, 1025, 1024, 16, 1);
        }

        private void ValidateRangeDecryption(BlobType type, int blobSize, int? blobOffset, int? length, int? verifyLength = null)
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                byte[] buffer = GetRandomBuffer(blobSize);

                ICloudBlob blob = GetCloudBlobReference(type, container);

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions options = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                // Upload the encrypted contents to the blob.
                MemoryStream stream = new MemoryStream(buffer);
                blob.UploadFromStream(stream, blobSize, null, options, null);

                // Download a range in the encrypted blob.
                MemoryStream outputStream = new MemoryStream();
                blob.DownloadRangeToStream(outputStream, blobOffset, length, null, options, null);

                outputStream.Seek(0, SeekOrigin.Begin);

                if (length.HasValue)
                {
                    if (verifyLength.HasValue)
                    {
                        Assert.AreEqual(verifyLength.Value, outputStream.Length);
                    }
                    else
                    {
                        Assert.AreEqual(length.Value, outputStream.Length);
                    }
                }

                // Compare that the decrypted contents match the input data.
                byte[] outputArray = outputStream.ToArray();

                for (int i = 0; i < outputArray.Length; i++)
                {
                    int bufferOffset = (int)(blobOffset.HasValue ? blobOffset : 0);
                    Assert.AreEqual(buffer[bufferOffset + i], outputArray[i]);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test the effects of blob stream's functionality with encryption")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        [DoNotParallelize]
        public void BlobEncryptedWriteStreamTest()
        {
            DoBlobEncryptedWriteStreamTest(BlobType.BlockBlob);
            DoBlobEncryptedWriteStreamTest(BlobType.PageBlob);
            DoBlobEncryptedWriteStreamTest(BlobType.AppendBlob);
        }

        private void DoBlobEncryptedWriteStreamTest(BlobType type)
        {
            byte[] buffer = GetRandomBuffer(8 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                ICloudBlob blob = null;

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                OperationContext opContext = new OperationContext();
                CloudBlobStream blobStream = null;
                if (type == BlobType.BlockBlob)
                {
                    blob = container.GetBlockBlobReference("blob1");
                    blob.StreamWriteSizeInBytes = 16 * 1024;
                    blobStream = ((CloudBlockBlob)blob).OpenWrite(null, uploadOptions, opContext);
                }
                else if (type == BlobType.PageBlob)
                {
                    blob = container.GetPageBlobReference("blob1");
                    blob.StreamWriteSizeInBytes = 16 * 1024;
                    blobStream = ((CloudPageBlob)blob).OpenWrite(40 * 1024, null, uploadOptions, opContext);
                }
                else
                {
                    blob = container.GetAppendBlobReference("blob1");
                    blob.StreamWriteSizeInBytes = 16 * 1024;
                    blobStream = ((CloudAppendBlob)blob).OpenWrite(true, null, uploadOptions, opContext);
                }

                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    using (blobStream)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            blobStream.Write(buffer, 0, buffer.Length);
                            wholeBlob.Write(buffer, 0, buffer.Length);
                        }

                        // Append and Page blobs have one extra call due to create.
                        if (type == BlobType.BlockBlob)
                        {
                            Assert.AreEqual(1, opContext.RequestResults.Count);
                        }
                        else
                        {
                            Assert.AreEqual(2, opContext.RequestResults.Count);
                        }

                        blobStream.Write(buffer, 0, buffer.Length);
                        wholeBlob.Write(buffer, 0, buffer.Length);

                        blobStream.Write(buffer, 0, buffer.Length);
                        wholeBlob.Write(buffer, 0, buffer.Length);

                        // Append and Page blobs have one extra call due to create.
                        if (type == BlobType.BlockBlob)
                        {
                            Assert.AreEqual(2, opContext.RequestResults.Count);
                        }
                        else
                        {
                            Assert.AreEqual(3, opContext.RequestResults.Count);
                        }

                        blobStream.Commit();

                        // Block blobs have an additional PutBlockList call.
                        Assert.AreEqual(4, opContext.RequestResults.Count);
                    }

                    Assert.AreEqual(4, opContext.RequestResults.Count);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob, null, uploadOptions, null);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test the effects of blob stream's functionality with encryption")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlobEncryptedWriteStreamTestAPM()
        {
            DoBlobEncryptedWriteStreamTestAPM(BlobType.BlockBlob);
            DoBlobEncryptedWriteStreamTestAPM(BlobType.PageBlob);
            DoBlobEncryptedWriteStreamTestAPM(BlobType.AppendBlob);
        }

        private void DoBlobEncryptedWriteStreamTestAPM(BlobType type)
        {
            byte[] buffer = GetRandomBuffer(8 * 1024);

            CloudBlobContainer container = GetRandomContainerReference();
            try
            {
                container.Create();

                ICloudBlob blob = null;

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                OperationContext opContext = new OperationContext();
                CloudBlobStream blobStream = null;
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result;
                    if (type == BlobType.BlockBlob)
                    {
                        blob = container.GetBlockBlobReference("blob1");
                        blob.StreamWriteSizeInBytes = 16 * 1024;
                        result = ((CloudBlockBlob)blob).BeginOpenWrite(null, uploadOptions, opContext, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blobStream = ((CloudBlockBlob)blob).EndOpenWrite(result);
                    }
                    else if (type == BlobType.PageBlob)
                    {
                        blob = container.GetPageBlobReference("blob1");
                        blob.StreamWriteSizeInBytes = 16 * 1024;
                        result = ((CloudPageBlob)blob).BeginOpenWrite(40 * 1024, null, uploadOptions, opContext, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blobStream = ((CloudPageBlob)blob).EndOpenWrite(result);
                    }
                    else
                    {
                        blob = container.GetAppendBlobReference("blob1");
                        blob.StreamWriteSizeInBytes = 16 * 1024;
                        result = ((CloudAppendBlob)blob).BeginOpenWrite(true, null, uploadOptions, opContext, ar => waitHandle.Set(), null);
                        waitHandle.WaitOne();
                        blobStream = ((CloudAppendBlob)blob).EndOpenWrite(result);
                    }
                }

                using (MemoryStream wholeBlob = new MemoryStream())
                {
                    using (blobStream)
                    {
                        using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                        {
                            IAsyncResult result;
                            for (int i = 0; i < 3; i++)
                            {
                                result = blobStream.BeginWrite(
                                    buffer,
                                    0,
                                    buffer.Length,
                                    ar => waitHandle.Set(),
                                    null);
                                waitHandle.WaitOne();
                                blobStream.EndWrite(result);
                                wholeBlob.Write(buffer, 0, buffer.Length);
                            }

                            // Append and Page blobs have one extra call due to create.
                            if (type == BlobType.BlockBlob)
                            {
                                Assert.AreEqual(1, opContext.RequestResults.Count);
                            }
                            else
                            {
                                Assert.AreEqual(2, opContext.RequestResults.Count);
                            }

                            result = blobStream.BeginWrite(
                                buffer,
                                0,
                                buffer.Length,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndWrite(result);
                            wholeBlob.Write(buffer, 0, buffer.Length);

                            result = blobStream.BeginWrite(
                                buffer,
                                0,
                                buffer.Length,
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndWrite(result);
                            wholeBlob.Write(buffer, 0, buffer.Length);

                            // Append and Page blobs have one extra call due to create.
                            if (type == BlobType.BlockBlob)
                            {
                                Assert.AreEqual(2, opContext.RequestResults.Count);
                            }
                            else
                            {
                                Assert.AreEqual(3, opContext.RequestResults.Count);
                            }

                            result = blobStream.BeginCommit(
                                ar => waitHandle.Set(),
                                null);
                            waitHandle.WaitOne();
                            blobStream.EndCommit(result);

                            Assert.AreEqual(4, opContext.RequestResults.Count);
                        }
                    }

                    Assert.AreEqual(4, opContext.RequestResults.Count);

                    using (MemoryStream downloadedBlob = new MemoryStream())
                    {
                        blob.DownloadToStream(downloadedBlob, null, uploadOptions);
                        TestHelper.AssertStreamsAreEqual(wholeBlob, downloadedBlob);
                    }
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Test rotating the encryption key on a blob - success case.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobEncryptionRotateSyncSuccess()
        {
            RunCloudBlobEncryptionRotateSync(true);
        }

        [TestMethod]
        [Description("Test rotating the encryption key on a blob - failure cases.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobEncryptionRotateSyncFailure()
        {
            RunCloudBlobEncryptionRotateSync(false);
        }

        public void RunCloudBlobEncryptionRotateSync(bool success)
        {
            Action<CloudBlob, MemoryStream, int, BlobRequestOptions> uploadCall = (blob, stream, size, options) =>
            {
                ((ICloudBlob)blob).UploadFromStream(stream, size, null, options, null);
            };
            Action<CloudBlob, AccessCondition, BlobRequestOptions> rotateCallMaxOverload = (blob, condition, options) =>
            {
                blob.RotateEncryptionKey(condition, options, null);
            };
            Action<CloudBlob, AccessCondition, BlobRequestOptions> rotateCallMinOverload = (blob, condition, options) =>
            {
                blob.ServiceClient.DefaultRequestOptions = options;
                blob.RotateEncryptionKey();
            };
            Action<CloudBlob, MemoryStream, BlobRequestOptions> downloadCall = (blob, stream, options) =>
            {
                blob.DownloadToStream(stream, null, options, null);
            };

            CloudBlobEncryptionRotateHelperAsync(uploadCall, rotateCallMaxOverload, downloadCall, success).GetAwaiter().GetResult();
            if (success)
            {
                CloudBlobEncryptionRotateHelperAsync(uploadCall, rotateCallMinOverload, downloadCall, success).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [Description("Test rotating the encryption key on a blob - success case.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobEncryptionRotateAPMSuccess()
        {
            RunCloudBlobEncryptionRotateAPM(true);
        }

        [TestMethod]
        [Description("Test rotating the encryption key on a blob - failure cases.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobEncryptionRotateAPMFailure()
        {
            RunCloudBlobEncryptionRotateAPM(false);
        }

        public void RunCloudBlobEncryptionRotateAPM(bool success)
        {
            Action<CloudBlob, MemoryStream, int, BlobRequestOptions> uploadCall = (blob, stream, size, options) =>
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    ICancellableAsyncResult result = ((ICloudBlob)blob).BeginUploadFromStream(
                                        stream, size, null, options, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    ((ICloudBlob)blob).EndUploadFromStream(result);
                }
            };
            Action<CloudBlob, AccessCondition, BlobRequestOptions> rotateCallMax = (blob, condition, options) =>
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    ICancellableAsyncResult result = blob.BeginRotateEncryptionKey(
                                        condition, options, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndRotateEncryptionKey(result);
                }
            };
            Action<CloudBlob, AccessCondition, BlobRequestOptions> rotateCallMin = (blob, condition, options) =>
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    blob.ServiceClient.DefaultRequestOptions = options;
                    ICancellableAsyncResult result = blob.BeginRotateEncryptionKey(ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndRotateEncryptionKey(result);
                }
            };
            Action<CloudBlob, MemoryStream, BlobRequestOptions> downloadCall = (blob, stream, options) =>
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    ICancellableAsyncResult result = blob.BeginDownloadToStream(
                                        stream, null, options, null, ar => waitHandle.Set(), null);
                    waitHandle.WaitOne();
                    blob.EndDownloadToStream(result);
                }
            };

            CloudBlobEncryptionRotateHelperAsync(uploadCall, rotateCallMax, downloadCall, success).GetAwaiter().GetResult();
            if (success)
            {
                CloudBlobEncryptionRotateHelperAsync(uploadCall, rotateCallMin, downloadCall, success).GetAwaiter().GetResult();
            }
        }

        [TestMethod]
        [Description("Test rotating the encryption key on a blob - success case.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobEncryptionRotateAsyncSuccess()
        {
            RunCloudBlobEncryptionRotateAsync(true).GetAwaiter().GetResult();
        }

        [TestMethod]
        [Description("Test rotating the encryption key on a blob - failure cases.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobEncryptionRotateAsyncFailure()
        {
            RunCloudBlobEncryptionRotateAsync(false).GetAwaiter().GetResult();
        }

        public async Task RunCloudBlobEncryptionRotateAsync(bool success)
        {
            Action<CloudBlob, MemoryStream, int, BlobRequestOptions> uploadCall = (blob, stream, size, options) =>
            {
                ((ICloudBlob)blob).UploadFromStreamAsync(stream, size, null, options, null).Wait();
            };
            Action<CloudBlob, AccessCondition, BlobRequestOptions> rotateCallMax = (blob, condition, options) =>
            {
                blob.RotateEncryptionKeyAsync(condition, options, null, CancellationToken.None).Wait();
            };
            Action<CloudBlob, AccessCondition, BlobRequestOptions> rotateCallMin1 = (blob, condition, options) =>
            {
                blob.ServiceClient.DefaultRequestOptions = options;
                blob.RotateEncryptionKeyAsync().Wait();
            };
            Action<CloudBlob, AccessCondition, BlobRequestOptions> rotateCallMin2 = (blob, condition, options) =>
            {
                blob.ServiceClient.DefaultRequestOptions = options;
                blob.RotateEncryptionKeyAsync(CancellationToken.None).Wait();
            };
            Action<CloudBlob, AccessCondition, BlobRequestOptions> rotateCallMin3 = (blob, condition, options) =>
            {
                blob.RotateEncryptionKeyAsync(condition, options, null).Wait();
            };
            Action<CloudBlob, MemoryStream, BlobRequestOptions> downloadCall = (blob, stream, options) =>
            {
                blob.DownloadToStreamAsync(stream, null, options, null).Wait();
            };
            List<Task> tasks = new List<Task>();
            tasks.Add(Task.Run(() => { CloudBlobEncryptionRotateHelperAsync(uploadCall, rotateCallMax, downloadCall, success); }));
            if (success)
            {
                tasks.Add(Task.Run(() => { CloudBlobEncryptionRotateHelperAsync(uploadCall, rotateCallMin1, downloadCall, success); }));
                tasks.Add(Task.Run(() => { CloudBlobEncryptionRotateHelperAsync(uploadCall, rotateCallMin2, downloadCall, success); }));
                tasks.Add(Task.Run(() => { CloudBlobEncryptionRotateHelperAsync(uploadCall, rotateCallMin3, downloadCall, success); }));
            }
            await Task.WhenAll(tasks.ToArray());
        }

        private async Task CloudBlobEncryptionRotateHelperAsync(Action<CloudBlob, MemoryStream, int, BlobRequestOptions> uploadCall, Action<CloudBlob, AccessCondition, BlobRequestOptions> rotateCall, Action<CloudBlob, MemoryStream, BlobRequestOptions> downloadCall, bool successCase)
        {
            List<Task> tasks = new List<Task>();
            if (successCase)
            {
                tasks.Add(Task.Run(() => { this.DoCloudBlobEncryptionRotateSuccessCase(BlobType.BlockBlob, false, uploadCall, rotateCall, downloadCall); }));
                tasks.Add(Task.Run(() => { this.DoCloudBlobEncryptionRotateSuccessCase(BlobType.PageBlob, false, uploadCall, rotateCall, downloadCall); }));
                tasks.Add(Task.Run(() => { this.DoCloudBlobEncryptionRotateSuccessCase(BlobType.AppendBlob, false, uploadCall, rotateCall, downloadCall); }));
                tasks.Add(Task.Run(() => { this.DoCloudBlobEncryptionRotateSuccessCase(BlobType.BlockBlob, true, uploadCall, rotateCall, downloadCall); }));
                tasks.Add(Task.Run(() => { this.DoCloudBlobEncryptionRotateSuccessCase(BlobType.PageBlob, true, uploadCall, rotateCall, downloadCall); }));
                tasks.Add(Task.Run(() => { this.DoCloudBlobEncryptionRotateSuccessCase(BlobType.AppendBlob, true, uploadCall, rotateCall, downloadCall); }));
            }
            else
            {
                tasks.Add(Task.Run(() => { this.DoCloudBlobEncryptionRotateFailureCases(BlobType.BlockBlob, false, uploadCall, rotateCall, downloadCall); }));
                tasks.Add(Task.Run(() => { this.DoCloudBlobEncryptionRotateFailureCases(BlobType.PageBlob, false, uploadCall, rotateCall, downloadCall); }));
                tasks.Add(Task.Run(() => { this.DoCloudBlobEncryptionRotateFailureCases(BlobType.AppendBlob, false, uploadCall, rotateCall, downloadCall); }));
                tasks.Add(Task.Run(() => { this.DoCloudBlobEncryptionRotateFailureCases(BlobType.BlockBlob, true, uploadCall, rotateCall, downloadCall); }));
                tasks.Add(Task.Run(() => { this.DoCloudBlobEncryptionRotateFailureCases(BlobType.PageBlob, true, uploadCall, rotateCall, downloadCall); }));
                tasks.Add(Task.Run(() => { this.DoCloudBlobEncryptionRotateFailureCases(BlobType.AppendBlob, true, uploadCall, rotateCall, downloadCall); }));
            }
            await Task.WhenAll(tasks.ToArray());
        }

        private void DoCloudBlobEncryptionRotateSuccessCase(BlobType type, bool partial, Action<CloudBlob, MemoryStream, int, BlobRequestOptions> uploadCall, Action<CloudBlob, AccessCondition, BlobRequestOptions> rotateCall, Action<CloudBlob, MemoryStream, BlobRequestOptions> downloadCall)
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                int size = 5 * 1024 * 1024;
                byte[] buffer = GetRandomBuffer(size);

                if (partial)
                {
                    size = 2 * 1024 * 1024;
                }

                CloudBlob blob;
                CloudBlob newBlob;
                if (type == BlobType.BlockBlob)
                {
                    blob = container.GetBlockBlobReference("blockblob");
                    newBlob = container.GetBlockBlobReference("blockblob");
                }
                else if (type == BlobType.PageBlob)
                {
                    blob = container.GetPageBlobReference("pageblob");
                    newBlob = container.GetPageBlobReference("pageblob");
                }
                else
                {
                    blob = container.GetAppendBlobReference("appendblob");
                    newBlob = container.GetAppendBlobReference("appendblob");
                }

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                MemoryStream stream;
                // Upload the encrypted contents to the blob.
                using (stream = new MemoryStream(buffer))
                {
                    uploadCall(blob, stream, size, uploadOptions);

                    // Ensure that the user stream is open.
                    Assert.IsTrue(stream.CanSeek);
                }

                // Create the new encryption key
                SymmetricKey aesKey2 = new SymmetricKey("symencryptionkey2");
                DictionaryKeyResolver resolverBothKeys = new DictionaryKeyResolver();
                resolverBothKeys.Add(aesKey);
                resolverBothKeys.Add(aesKey2);
                DictionaryKeyResolver resolverSecondKeyOnly = new DictionaryKeyResolver();
                resolverSecondKeyOnly.Add(aesKey2);

                // Create a request options object that contains both the new key, and a resolver capable of resolving the old key.
                BlobEncryptionPolicy rotatePolicy = new BlobEncryptionPolicy(aesKey2, resolverBothKeys);
                BlobRequestOptions rotateOptions = new BlobRequestOptions() { EncryptionPolicy = rotatePolicy };

                newBlob.FetchAttributes();
                rotateCall(newBlob, null, rotateOptions);

                // Download the encrypted blob.
                // Create the decryption policy to be used for download. There is no need to specify the
                // key when the policy is only going to be used for downloads. Resolver is sufficient.
                BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, resolverSecondKeyOnly);

                // Set the decryption policy on the request options.
                BlobRequestOptions downloadOptions = new BlobRequestOptions() { EncryptionPolicy = downloadPolicy };

                // Download and decrypt the encrypted contents from the blob.
                MemoryStream outputStream = new MemoryStream();
                downloadCall(blob, outputStream, downloadOptions);

                // Ensure that the user stream is open.
                outputStream.Seek(0, SeekOrigin.Begin);

                // Compare that the decrypted contents match the input data.
                byte[] outputArray = outputStream.ToArray();
                TestHelper.AssertBuffersAreEqualUptoIndex(outputArray, buffer, size - 1);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        private void DoCloudBlobEncryptionRotateFailureCases(BlobType type, bool partial, Action<CloudBlob, MemoryStream, int, BlobRequestOptions> uploadCall, Action<CloudBlob, AccessCondition, BlobRequestOptions> rotateCall, Action<CloudBlob, MemoryStream, BlobRequestOptions> downloadCall)
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                int size = 5 * 1024 * 1024;
                byte[] buffer = GetRandomBuffer(size);

                if (partial)
                {
                    size = 2 * 1024 * 1024;
                }

                CloudBlob blob;
                CloudBlob newBlob;
                if (type == BlobType.BlockBlob)
                {
                    blob = container.GetBlockBlobReference("blockblob");
                    newBlob = container.GetBlockBlobReference("blockblob");
                }
                else if (type == BlobType.PageBlob)
                {
                    blob = container.GetPageBlobReference("pageblob");
                    newBlob = container.GetPageBlobReference("pageblob");
                }
                else
                {
                    blob = container.GetAppendBlobReference("appendblob");
                    newBlob = container.GetAppendBlobReference("appendblob");
                }

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                MemoryStream stream;
                // Upload the encrypted contents to the blob.
                using (stream = new MemoryStream(buffer))
                {
                    uploadCall(blob, stream, size, uploadOptions);

                    // Ensure that the user stream is open.
                    Assert.IsTrue(stream.CanSeek);
                }

                // Create the new encryption key
                SymmetricKey aesKey2 = new SymmetricKey("symencryptionkey2");
                DictionaryKeyResolver resolverBothKeys = new DictionaryKeyResolver();
                resolverBothKeys.Add(aesKey);
                resolverBothKeys.Add(aesKey2);
                DictionaryKeyResolver resolverSecondKeyOnly = new DictionaryKeyResolver();
                resolverSecondKeyOnly.Add(aesKey2);

                // Create a request options object that contains both the new key, and a resolver capable of resolving the old key.
                BlobEncryptionPolicy rotatePolicy = new BlobEncryptionPolicy(aesKey2, resolverBothKeys);
                BlobRequestOptions rotateOptions = new BlobRequestOptions() { EncryptionPolicy = rotatePolicy };

                // Test that key rotation will fail if we don't have the encryption metadata
                TestHelper.ExpectedException<InvalidOperationException>(
                    () => rotateCall(newBlob, null, rotateOptions),
                    "Key rotation should fail if encryption metadata is not present.");

                newBlob.FetchAttributes();

                // Test that we fail client-side if the RequestOptions either doesn't contain the new encryption key, or doesn't 
                // contain a resolver capable of resolving the old key
                TestHelper.ExpectedException<ArgumentException>(
                    () => rotateCall(newBlob, null, new BlobRequestOptions() { EncryptionPolicy = new BlobEncryptionPolicy(null, resolverBothKeys) }),
                    "Key rotation should fail when no new key is provided.");
                TestHelper.ExpectedException<ArgumentException>(
                    () => rotateCall(newBlob, null, new BlobRequestOptions() { EncryptionPolicy = new BlobEncryptionPolicy(aesKey2, null) }),
                    "Key rotation should fail when no resolver.");
                TestHelper.ExpectedException<ArgumentException>(
                    () =>
                    {
                        try
                        {
                            rotateCall(newBlob, null, new BlobRequestOptions() { EncryptionPolicy = new BlobEncryptionPolicy(aesKey2, resolverSecondKeyOnly) });
                        }
                        catch (AggregateException ex) // Unwrap the Aggregate exception that's thrown from the async case.
                        {
                            throw ex.InnerException;
                        }
                    },
                    "Key rotation should fail when the resolver is incapable of resolving the original key.");

                // Test that the operation fails if the ETag is incorrect
                blob.FetchAttributes();
                blob.Metadata[@"sample"] = @"sample";
                blob.SetMetadata();
                TestHelper.ExpectedException<StorageException>(
                    () =>
                    {
                        try
                        {
                            rotateCall(newBlob, null, rotateOptions);
                        }
                        catch (AggregateException ex) // Unwrap the Aggregate exception that's thrown from the async case.
                        {
                            throw ex.InnerException;
                        }
                    },
                    "Key rotation should fail if Etag is incorrect.");

                // Test that the operation should fail if the user passes in an AccessCondition that's If-Match or If-Modified-Since
                AccessCondition badCondition = AccessCondition.GenerateIfMatchCondition("anyEtag");
                TestHelper.ExpectedException<ArgumentException>(
                    () => rotateCall(newBlob, badCondition, rotateOptions),
                    "Key rotation should fail if an If-Match Access Condition is used.");

                badCondition = AccessCondition.GenerateIfModifiedSinceCondition(DateTimeOffset.Now);
                TestHelper.ExpectedException<ArgumentException>(
                    () => rotateCall(newBlob, badCondition, rotateOptions),
                    "Key rotation should fail if an If-Modified-Since Access Condition is used.");
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Update operations on blob should throw if encryption policy is set.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlobUpdateShouldThrowWithEncryption()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                byte[] buffer = GetRandomBuffer(16 * 1024);

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference("blockblob");
                    TestHelper.ExpectedException<InvalidOperationException>(
                        () => blockBlob.PutBlock(Convert.ToBase64String(Guid.NewGuid().ToByteArray()), stream, null, null, uploadOptions, null),
                        "PutBlock does not support encryption.");

                    CloudAppendBlob appendBlob = container.GetAppendBlobReference("appendblob");
                    TestHelper.ExpectedException<InvalidOperationException>(
                        () => appendBlob.AppendBlock(stream, null, null, uploadOptions, null),
                        "AppendBlock does not support encryption.");

                    CloudPageBlob pageBlob = container.GetPageBlobReference("pageblob");
                    TestHelper.ExpectedException<InvalidOperationException>(
                        () => pageBlob.WritePages(stream, 0, null, null, uploadOptions, null),
                        "WritePages does not support encryption.");

                    TestHelper.ExpectedException<InvalidOperationException>(
                        () => pageBlob.ClearPages(0, 512, null, uploadOptions, null),
                        "ClearPages does not support encryption.");
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate that default request options correctly set encryption policy.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.FuntionalTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlobUploadWorksWithDefaultRequestOptions()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            byte[] buffer = GetRandomBuffer(16 * 1024);

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            // Create the encryption policy to be used for upload.
            BlobEncryptionPolicy policy = new BlobEncryptionPolicy(aesKey, null);

            // Set the encryption policy on the request options.
            BlobRequestOptions options = new BlobRequestOptions() { EncryptionPolicy = policy };

            // Set default request options
            container.ServiceClient.DefaultRequestOptions = options;

            try
            {
                container.Create();

                using (MemoryStream stream = new MemoryStream(buffer))
                {
                    CloudBlockBlob blockBlob = container.GetBlockBlobReference("blockblob");
                    blockBlob.UploadFromStream(stream, buffer.Length);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate encryption/decryption with RequireEncryption flag.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobEncryptionWithStrictMode()
        {
            this.DoCloudBlobEncryptionWithStrictMode(BlobType.BlockBlob);
            this.DoCloudBlobEncryptionWithStrictMode(BlobType.PageBlob);
        }

        private void DoCloudBlobEncryptionWithStrictMode(BlobType type)
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                int size = 5 * 1024 * 1024;
                byte[] buffer = GetRandomBuffer(size);

                ICloudBlob blob;

                if (type == BlobType.BlockBlob)
                {
                    blob = container.GetBlockBlobReference("blob1");
                }
                else
                {
                    blob = container.GetPageBlobReference("blob1");
                }

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the resolver to be used for unwrapping.
                DictionaryKeyResolver resolver = new DictionaryKeyResolver();
                resolver.Add(aesKey);

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions() { EncryptionPolicy = uploadPolicy };

                // Set RequireEncryption flag to true.
                uploadOptions.RequireEncryption = true;

                // Upload an encrypted blob with the policy set.
                MemoryStream stream = new MemoryStream(buffer);
                blob.UploadFromStream(stream, size, null, uploadOptions, null);

                // Upload the blob when RequireEncryption is true and no policy is set. This should throw an error.
                uploadOptions.EncryptionPolicy = null;

                stream = new MemoryStream(buffer);
                TestHelper.ExpectedException<InvalidOperationException>(
                    () => blob.UploadFromStream(stream, size, null, uploadOptions, null),
                    "Not specifying a policy when RequireEnryption is set to true should throw.");

                // Create the encryption policy to be used for download.
                BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, resolver);

                // Set the encryption policy on the request options.
                BlobRequestOptions downloadOptions = new BlobRequestOptions() { EncryptionPolicy = downloadPolicy };

                // Set RequireEncryption flag to true.
                downloadOptions.RequireEncryption = true;

                // Download the encrypted blob.
                MemoryStream outputStream = new MemoryStream();
                blob.DownloadToStream(outputStream, null, downloadOptions, null);

                blob.Metadata.Clear();

                // Upload a plain text blob.
                stream = new MemoryStream(buffer);
                blob.UploadFromStream(stream, size);

                // Try to download an encrypted blob with RequireEncryption set to true. This should throw.
                outputStream = new MemoryStream();
                TestHelper.ExpectedException<StorageException>(
                    () => blob.DownloadToStream(outputStream, null, downloadOptions, null),
                    "Downloading with RequireEncryption set to true and no metadata on the service should fail.");

                // Set RequireEncryption to false and download.
                downloadOptions.RequireEncryption = false;
                blob.DownloadToStream(outputStream, null, downloadOptions, null);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate partial blob encryption with RequireEncryption flag.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobEncryptionWithStrictModeOnPartialBlob()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            int size = 5 * 1024 * 1024;
            byte[] buffer = GetRandomBuffer(size);

            ICloudBlob blob;
            MemoryStream stream = new MemoryStream(buffer);
            String blockId = Convert.ToBase64String(Guid.NewGuid().ToByteArray());

            BlobRequestOptions options = new BlobRequestOptions()
            {
                RequireEncryption = true
            };

            blob = container.GetBlockBlobReference("blob1");
            try
            {
                ((CloudBlockBlob)blob).PutBlock(blockId, stream, null, null, options, null);
                Assert.Fail("PutBlock with RequireEncryption on should fail.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(ex.Message, SR.EncryptionPolicyMissingInStrictMode);
            }

            blob = container.GetPageBlobReference("blob1");
            try
            {
                ((CloudPageBlob)blob).WritePages(stream, 0, null, null, options, null);
                Assert.Fail("WritePages with RequireEncryption on should fail.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(ex.Message, SR.EncryptionPolicyMissingInStrictMode);
            }

            blob = container.GetAppendBlobReference("blob1");
            try
            {
                ((CloudAppendBlob)blob).AppendBlock(stream, null, null, options, null);
                Assert.Fail("AppendBlock with RequireEncryption on should fail.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(ex.Message, SR.EncryptionPolicyMissingInStrictMode);
            }

            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");
            options.EncryptionPolicy = new BlobEncryptionPolicy(aesKey, null);

            blob = container.GetBlockBlobReference("blob1");
            try
            {
                ((CloudBlockBlob)blob).PutBlock(blockId, stream, null, null, options, null);
                Assert.Fail("PutBlock with an EncryptionPolicy should fail.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(ex.Message, SR.EncryptionNotSupportedForOperation);
            }

            blob = container.GetPageBlobReference("blob1");
            try
            {
                ((CloudPageBlob)blob).WritePages(stream, 0, null, null, options, null);
                Assert.Fail("WritePages with an EncryptionPolicy should fail.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(ex.Message, SR.EncryptionNotSupportedForOperation);
            }

            blob = container.GetAppendBlobReference("blob1");
            try
            {
                ((CloudAppendBlob)blob).AppendBlock(stream, null, null, options, null);
                Assert.Fail("AppendBlock with an EncryptionPolicy should fail.");
            }
            catch (InvalidOperationException ex)
            {
                Assert.AreEqual(ex.Message, SR.EncryptionNotSupportedForOperation);
            }
        }

        private static ICloudBlob GetCloudBlobReference(BlobType type, CloudBlobContainer container)
        {
            ICloudBlob blob;
            if (type == BlobType.BlockBlob)
            {
                blob = container.GetBlockBlobReference("blockblob");
            }
            else if (type == BlobType.PageBlob)
            {
                blob = container.GetPageBlobReference("pageblob");
            }
            else
            {
                blob = container.GetAppendBlobReference("appendblob");
            }

            return blob;
        }

        [TestMethod]
        [Description("Validate that the bug where we did not encrypt data if certain access conditions were specified no longer exists.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobEncryptionOpenWriteStreamAPMWithAccessCondition()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                int size = (int)(1 * Constants.MB);
                container.Create();
                byte[] buffer = GetRandomBuffer(size);

                CloudBlockBlob blob = container.GetBlockBlobReference("blockblob");
                blob.StreamWriteSizeInBytes = (int)(4 * Constants.MB);

                blob.UploadText("Sample initial text");

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the resolver to be used for unwrapping.
                DictionaryKeyResolver resolver = new DictionaryKeyResolver();
                resolver.Add(aesKey);

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                BlobRequestOptions uploadOptions = new BlobRequestOptions()
                {
                    EncryptionPolicy = uploadPolicy,
                    RequireEncryption = true
                };

                AccessCondition accessCondition = AccessCondition.GenerateIfMatchCondition(blob.Properties.ETag);

                using (MemoryStream stream = new NonSeekableMemoryStream(buffer))
                {
                    using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                    {
                        blob.EndUploadFromStream(blob.BeginUploadFromStream(
                                            stream, size, accessCondition, uploadOptions, null, ar => waitHandle.Set(), null));
                    }
                }

                // Download the encrypted blob.
                // Create the decryption policy to be used for download. There is no need to specify the
                // key when the policy is only going to be used for downloads. Resolver is sufficient.
                BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, resolver);

                // Set the decryption policy on the request options.
                BlobRequestOptions downloadOptions = new BlobRequestOptions()
                {
                    EncryptionPolicy = downloadPolicy,
                    RequireEncryption = true
                };

                // Download and decrypt the encrypted contents from the blob.
                MemoryStream outputStream = new MemoryStream();
                blob.DownloadToStream(outputStream, null, downloadOptions, null);

                // Ensure that the user stream is open.
                outputStream.Seek(0, SeekOrigin.Begin);

                // Compare that the decrypted contents match the input data.
                byte[] outputArray = outputStream.ToArray();
                TestHelper.AssertBuffersAreEqualUptoIndex(outputArray, buffer, size - 1);
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Attempt to download unencrypted blob with encryption policy.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore)]
        [TestCategory(TenantTypeCategory.DevFabric)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobDownloadUnencryptedBlobWithEncryptionPolicy()
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                CloudBlockBlob blob = container.GetBlockBlobReference("blockblob");

                string message = "Sample initial text";
                blob.UploadText(message);

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the resolver to be used for unwrapping.
                DictionaryKeyResolver resolver = new DictionaryKeyResolver();
                resolver.Add(aesKey);

                // Download the encrypted blob.
                // Create the decryption policy to be used for download. There is no need to specify the
                // key when the policy is only going to be used for downloads. Resolver is sufficient.
                BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, resolver);

                // Set the decryption policy on the request options.
                BlobRequestOptions downloadOptions = new BlobRequestOptions()
                {
                    EncryptionPolicy = downloadPolicy,
                    RequireEncryption = true
                };

                try
                {
                    blob.DownloadText(null, null, downloadOptions, null);
                }
                catch (StorageException e)
                {
                    Assert.AreEqual(SR.EncryptionDataNotPresentError, e.Message);
                }

                byte[] buffer = new byte[message.Length + 5];
                try
                {
                    blob.DownloadRangeToByteArray(buffer, 0, 0, message.Length, null, downloadOptions, null);
                }
                catch (StorageException e)
                {
                    Assert.AreEqual(SR.EncryptionDataNotPresentError, e.Message);
                }
            }
            finally
            {
                container.DeleteIfExists();
            }
        }

        [TestMethod]
        [Description("Validate that we will do a single PutBlob call when uplaoding an encrypted blob if the data is short enough.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudBlobEncryptionCountOperations()
        {
            BlobRequestOptions options = new BlobRequestOptions();
            options.SingleBlobUploadThresholdInBytes = 8 * Constants.MB - 3;  // We should test a non-multiple of 16
            options.ParallelOperationThreadCount = 1;

            List<Task> tasks = new List<Task>();
            Boolean[] bothBools = new Boolean[] { true, false };

            foreach (bool isAPM in bothBools)
            {
                foreach (bool calculateMD5 in bothBools)
                foreach (bool calculateCRC64 in bothBools)
                {
                    if (calculateMD5 && calculateCRC64) continue;

                    options.ChecksumOptions.StoreContentMD5 = calculateMD5;
                    options.ChecksumOptions.UseTransactionalMD5 = calculateMD5;
                    options.ChecksumOptions.DisableContentMD5Validation = !calculateMD5;

                    options.ChecksumOptions.StoreContentCRC64 = calculateCRC64;
                    options.ChecksumOptions.UseTransactionalCRC64 = calculateCRC64;
                    options.ChecksumOptions.DisableContentCRC64Validation = !calculateCRC64;

                    // We need to make a local copy of the 'isAPM' variable, otherwise the below
                    // lambdas will all use the final value of 'isAPM'
                    bool localIsAPM = isAPM;

                    // Make a copy of the options object, so that we can run the tests in parallel.
                    BlobRequestOptions localOptions = new BlobRequestOptions(options);
                    tasks.Add(Task.Run(() =>
                    {
                        this.CountOperationsHelper(10, 1, true, localIsAPM, localOptions);
                    }));
                    tasks.Add(Task.Run(() =>
                    {
                        this.CountOperationsHelper((int)(1 * Constants.MB), 1, true, localIsAPM, localOptions);
                    }));

                    // This one should not call put, because encryption padding will put it over length.
                    tasks.Add(Task.Run(() =>
                        {
                            this.CountOperationsHelper((int)(options.SingleBlobUploadThresholdInBytes - 2), 3, true, localIsAPM, localOptions);
                        }));
                    tasks.Add(Task.Run(() =>
                    {
                        this.CountOperationsHelper((int)(13 * Constants.MB), 5, true, localIsAPM, localOptions);
                    }));
                }
            }
            Task.WaitAll(tasks.ToArray());
        }

        [TestMethod]
        [Description("Validate that decryption functions correctly even if bytes are being written in very small chunks.")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void BlobDecryptStreamSmallWriteTest()
        {
            // Create the Key to be used for wrapping.
            SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

            // Create the resolver to be used for unwrapping.
            DictionaryKeyResolver resolver = new DictionaryKeyResolver();
            resolver.Add(aesKey);

            // Create the encryption policy to be used for upload.
            BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

            byte[] rawData = GetRandomBuffer(1029);
            MemoryStream encryptedDataStream = new MemoryStream();

            Dictionary<string, string> tempMetadata = new Dictionary<string, string>();
            ICryptoTransform transform = uploadPolicy.CreateAndSetEncryptionContext(tempMetadata, false /* noPadding */);
            CryptoStream cryptoStream = new CryptoStream(encryptedDataStream, transform, CryptoStreamMode.Write);

            cryptoStream.Write(rawData, 0, rawData.Length);
            cryptoStream.FlushFinalBlock();

            encryptedDataStream.Seek(0, SeekOrigin.Begin);
            byte[] encryptedData = encryptedDataStream.ToArray();

            // Download & decrypt some small subset of the data
            int startByte = 327;
            int length = 184;
            int originalStart = startByte - (startByte % 16) - 16; // Account for filling an entire block on the start side, plus an extra block as an IV.
            int finalbyte = startByte + length;
            finalbyte += (16 - finalbyte % 16); // Extend to fill an entire block on the end side

            // Each operation will try writing only one byte at a time.  Decryption should still succeed
            // in this scenario, buffering as necessary.
            List<Action<BlobDecryptStream, byte[], int>> writeOperations = new List<Action<BlobDecryptStream, byte[], int>>()
            {
                (str, arr, j) => str.Write(arr, j, 1),
                (str, arr, j) => str.EndWrite(str.BeginWrite(arr, j, 1, null, null)),
                (str, arr, j) => str.WriteAsync(arr, j, 1).Wait()
            };

            foreach (Action<BlobDecryptStream, byte[], int> op in writeOperations)
            {
                MemoryStream targetStream = new MemoryStream();

                using (BlobDecryptStream streamToTest = new BlobDecryptStream(targetStream, tempMetadata, length, startByte % 16, true, true, uploadPolicy, false))
                {
                    for (int i = originalStart; i < finalbyte; i++)
                    {
                        op(streamToTest, encryptedData, i);
                    }
                }
                targetStream.Seek(0, SeekOrigin.Begin);
                MemoryStream src = new MemoryStream(rawData, startByte, length);
                TestHelper.AssertStreamsAreEqual(src, targetStream);
            }
        }

        private void CountOperationsHelper(int size, int targetUploadOperations, bool streamSeekable, bool isAPM, BlobRequestOptions options)
        {
            CloudBlobContainer container = GetRandomContainerReference();

            try
            {
                container.Create();
                byte[] buffer = GetRandomBuffer(size);

                CloudBlockBlob blob = container.GetBlockBlobReference("blockblob");
                blob.StreamWriteSizeInBytes = (int)(4 * Constants.MB);

                // Create the Key to be used for wrapping.
                SymmetricKey aesKey = new SymmetricKey("symencryptionkey");

                // Create the resolver to be used for unwrapping.
                DictionaryKeyResolver resolver = new DictionaryKeyResolver();
                resolver.Add(aesKey);

                // Create the encryption policy to be used for upload.
                BlobEncryptionPolicy uploadPolicy = new BlobEncryptionPolicy(aesKey, null);

                // Set the encryption policy on the request options.
                options.EncryptionPolicy = uploadPolicy;
                OperationContext opContext = new OperationContext();

                int uploadCount = 0;
                opContext.SendingRequest += (sender, e) => uploadCount++;

                using (MemoryStream stream = streamSeekable ? new MemoryStream(buffer) : new NonSeekableMemoryStream(buffer))
                {
                    if (isAPM)
                    {
                        using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                        {
                            blob.EndUploadFromStream(blob.BeginUploadFromStream(
                                                stream, size, null, options, opContext, ar => waitHandle.Set(), null));
                        }
                    }
                    else
                    {
                        blob.UploadFromStream(stream, size, null, options, opContext);
                    }

                    // Ensure that the user stream is open if it's seekable.
                    if (streamSeekable)
                    {
                        Assert.IsTrue(stream.CanSeek);
                    }
                }

                // Download the encrypted blob.
                // Create the decryption policy to be used for download. There is no need to specify the
                // key when the policy is only going to be used for downloads. Resolver is sufficient.
                BlobEncryptionPolicy downloadPolicy = new BlobEncryptionPolicy(null, resolver);

                // Set the decryption policy on the request options.
                BlobRequestOptions downloadOptions = new BlobRequestOptions() { EncryptionPolicy = downloadPolicy };

                // Download and decrypt the encrypted contents from the blob.
                MemoryStream outputStream = new MemoryStream();
                blob.DownloadToStream(outputStream, null, downloadOptions, null);

                // Ensure that the user stream is open.
                outputStream.Seek(0, SeekOrigin.Begin);

                // Compare that the decrypted contents match the input data.
                byte[] outputArray = outputStream.ToArray();
                TestHelper.AssertBuffersAreEqualUptoIndex(outputArray, buffer, size - 1);
                Assert.AreEqual(targetUploadOperations, uploadCount, "Incorrect number of operations in encrypted blob upload.");
            }
            finally
            {
                container.DeleteIfExists();
            }
        }
    }
}