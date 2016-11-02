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

namespace Microsoft.WindowsAzure.Storage.Blob
{
    using Microsoft.Azure.KeyVault;
    using Microsoft.Azure.KeyVault.Core;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
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
            this.DoCloudBlobEncryption(BlobType.BlockBlob, false);
            this.DoCloudBlobEncryption(BlobType.PageBlob, false);
            this.DoCloudBlobEncryption(BlobType.AppendBlob, false);

            this.DoCloudBlobEncryption(BlobType.BlockBlob, true);
            this.DoCloudBlobEncryption(BlobType.PageBlob, true);
            this.DoCloudBlobEncryption(BlobType.AppendBlob, true);
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
                    blob.UploadFromStream(stream, size, null, uploadOptions, null);

                    // Ensure that the user stream is open.
                    Assert.IsTrue(stream.CanSeek);
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
            DoCloudBlobEncryptionAPM(BlobType.BlockBlob, false);
            DoCloudBlobEncryptionAPM(BlobType.PageBlob, false);
            DoCloudBlobEncryptionAPM(BlobType.AppendBlob, false);

            DoCloudBlobEncryptionAPM(BlobType.BlockBlob, true);
            DoCloudBlobEncryptionAPM(BlobType.PageBlob, true);
            DoCloudBlobEncryptionAPM(BlobType.AppendBlob, true);
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

            DoCloudBlockBlobEncryptionValidateWrappers(aesKey, resolver);
            DoCloudBlockBlobEncryptionValidateWrappers(rsaKey, resolver);
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

            Boolean[] bothBools = new Boolean[] { true, false };

            foreach (bool isAPM in bothBools)
            {
                foreach (bool calculateMD5 in bothBools)
                {
                    options.StoreBlobContentMD5 = calculateMD5;
                    options.UseTransactionalMD5 = calculateMD5;
                    options.DisableContentMD5Validation = !calculateMD5;
                    this.CountOperationsHelper(10, 1, true, isAPM, options);
                    this.CountOperationsHelper((int)(1 * Constants.MB), 1, true, isAPM, options);

                    // This one should not call put, because encryption padding will put it over length.
                    this.CountOperationsHelper((int)(options.SingleBlobUploadThresholdInBytes - 2), 3, true, isAPM, options);
                    this.CountOperationsHelper((int)(13 * Constants.MB), 5, true, isAPM, options);
                }
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