// -----------------------------------------------------------------------------------------
// <copyright file="BlobResponseTests.cs" company="Microsoft">
//    Copyright Microsoft Corporation
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

using Microsoft.Azure.Storage.Core;
using Microsoft.Azure.Storage.Shared.Protocol;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Security.Cryptography;

namespace Microsoft.Azure.Storage.Blob.Protocol
{
    [TestClass]
    public class BlobResponseTests : TestBase
    {
        [TestMethod]
        [Description("ValidateCPKHeaders when options.CustomerProvidedKey is null")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ValidateCPKHeadersNullCPK()
        {
            // Arrange
            var response = new HttpResponseMessage();
            var options = new BlobRequestOptions();

            // Act
            BlobResponse.ValidateCPKHeaders(response, options, true);

            // Assert
            // No exception thrown
        }

        [TestMethod]
        [Description("ValidateCPKHeaders when response encryption key SHA does not match CPK encyption key SHA")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ValidateCPKHeadersKeySHAMissmatch()
        {
            // Arrange
            var response = new HttpResponseMessage();
            var options = BuildBlobRequestOptions();

            // Act
            TestHelper.ExpectedException<StorageException>(() => BlobResponse.ValidateCPKHeaders(response, options, true), 
                SR.ClientProvidedKeyBadHash);
        }

        [TestMethod]
        [Description("ValidateCPKHeaders when request was an upload and response ms-request-server-encrypted header is not true")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ValidateCPKHeadersUploadNotServiceRequestEncrypted()
        {
            // Arrange
            var options = BuildBlobRequestOptions();
            var response = new HttpResponseMessage();
            response.Headers.Add(Constants.HeaderConstants.ClientProvidedEncyptionKeyHash, options.CustomerProvidedKey.KeySHA256);

            // Act
            TestHelper.ExpectedException<StorageException>(() => BlobResponse.ValidateCPKHeaders(response, options, true),
                SR.ClientProvidedKeyEncryptionFailure);
        }

        [TestMethod]
        [Description("ValidateCPKHeaders when request was not an upload and response ms-server-encrypted header is not true")]
        [TestCategory(ComponentCategory.Blob)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ValidateCPKHeadersNotUploadNotServerEncrypted()
        {
            // Arrange
            var options = BuildBlobRequestOptions();
            var response = new HttpResponseMessage();
            response.Headers.Add(Constants.HeaderConstants.ClientProvidedEncyptionKeyHash, options.CustomerProvidedKey.KeySHA256);

            // Act
            TestHelper.ExpectedException<StorageException>(() => BlobResponse.ValidateCPKHeaders(response, options, false),
                SR.ClientProvidedKeyEncryptionFailure);
        }

        private BlobRequestOptions BuildBlobRequestOptions()
        {
            using (var aes = Aes.Create())
            {
                return new BlobRequestOptions
                {
                    CustomerProvidedKey = new BlobCustomerProvidedKey(aes.Key)
                };
            }
        }
    }
}
