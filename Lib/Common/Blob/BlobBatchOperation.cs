//-----------------------------------------------------------------------
// <copyright file="BlobBatchOperation.cs" company="Microsoft">
//    Copyright 2019 Microsoft Corporation
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
//-----------------------------------------------------------------------

using Microsoft.Azure.Storage.Blob.Protocol;
using Microsoft.Azure.Storage.Core;
using Microsoft.Azure.Storage.Core.Executor;
using Microsoft.Azure.Storage.Core.Util;
using Microsoft.Azure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Storage.Blob
{
    /// <summary>
    /// Defines a set of SetTier suboperations to be executed as a single batched request.
    /// </summary>
    public sealed class BlobSetTierBatchOperation : BatchOperation
    {
        /// <summary>
        /// Adds an operation to be submitted as part of the batch.
        /// </summary>
        /// <param name="blockBlob">The <see cref="CloudBlockBlob"/> whose tier will be set.</param>
        /// <param name="standardBlobTier">A <see cref="StandardBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="blobRequestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        public void AddSubOperation(CloudBlockBlob blockBlob, StandardBlobTier standardBlobTier, AccessCondition accessCondition = default(AccessCondition), BlobRequestOptions blobRequestOptions = default(BlobRequestOptions))
        {
            CommonUtility.AssertInBounds("operationCount", this.Operations.Count, 0, Constants.MaxSubOperationPerBatch - 1);
            CommonUtility.AssertNotNull("blockBlob", blockBlob);
            CommonUtility.AssertNotNull("standardBlobTier", standardBlobTier);

            this.Operations.Add(blockBlob.SetStandardBlobTierImpl(standardBlobTier, default(RehydratePriority?), accessCondition, blobRequestOptions ?? BlobRequestOptions.BaseDefaultRequestOptions));
        }

        /// <summary>
        /// Adds an operation be submitted as part of the batch. 
        /// </summary>
        /// <param name="pageBlob">The <see cref="CloudPageBlob"/> whose tier will be set.</param>
        /// <param name="premiumPageBlobTier">A <see cref="PremiumPageBlobTier"/> representing the tier to set.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="blobRequestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        public void AddSubOperation(CloudPageBlob pageBlob, PremiumPageBlobTier premiumPageBlobTier, AccessCondition accessCondition = default(AccessCondition), BlobRequestOptions blobRequestOptions = default(BlobRequestOptions))
        {
            CommonUtility.AssertInBounds("operationCount", this.Operations.Count, 0, Constants.MaxSubOperationPerBatch - 1);
            CommonUtility.AssertNotNull("pageBlob", pageBlob);
            CommonUtility.AssertNotNull("premiumPageBlobTier", premiumPageBlobTier);

            this.Operations.Add(pageBlob.SetBlobTierImpl(premiumPageBlobTier, blobRequestOptions ?? BlobRequestOptions.BaseDefaultRequestOptions));
        }
    }


    /// <summary>
    /// Defines a set of SetTier suboperations to be executed as a single batched request.
    /// </summary>
    public sealed class BlobDeleteBatchOperation : BatchOperation
    {
        /// <summary>
        /// Adds an operation to be submitted as part of the batch.
        /// </summary>
        /// <param name="blob">The <see cref="CloudBlob"/> to be deleted.</param>
        /// <param name="deleteSnapshotsOption">A <see cref="DeleteSnapshotsOption"/> object indicating whether to only delete the blob, to delete the blob and all snapshots, or to only delete the snapshots.</param>
        /// <param name="accessCondition">An <see cref="AccessCondition"/> object that represents the condition that must be met in order for the request to proceed. If <c>null</c>, no condition is used.</param>
        /// <param name="blobRequestOptions">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        public void AddSubOperation(CloudBlob blob, DeleteSnapshotsOption deleteSnapshotsOption = DeleteSnapshotsOption.None, AccessCondition accessCondition = default(AccessCondition), BlobRequestOptions blobRequestOptions = default(BlobRequestOptions))
        {
            CommonUtility.AssertInBounds("operationCount", this.Operations.Count, 0, Constants.MaxSubOperationPerBatch - 1);
            CommonUtility.AssertNotNull("blockBlob", blob);

            this.Operations.Add(blob.DeleteBlobImpl(blob.attributes, deleteSnapshotsOption, accessCondition, blobRequestOptions ?? BlobRequestOptions.BaseDefaultRequestOptions));
        }
    }

    /// <summary>
    /// Defines the basic structure of constructing batched requests.  Specific operations are supported by specific implementations.
    /// </summary>
    /// <remarks>
    /// Batch operations allow for preparing a large number of requests and sending them all in one operation. Upon reaching the service, each suboperation is 
    /// treated independently. In particular, this means the batch operation is not atomic; some suboperations may fail while others succeed. If any
    /// suboperation fails, an exception will be thrown containing a list of both successful and failed responses with relevant information on each. 
    /// </remarks>
    public abstract class BatchOperation
    {
        /// <summary>
        /// The list of sub-operations.
        /// </summary>
        internal List<RESTCommand<NullType>> Operations { get; } = new List<RESTCommand<NullType>>();

        /// <summary>
        /// The batch id which acts as the separator between sub requests. Must also be present in the headers of the uber request.
        /// </summary>
        internal string BatchID { get; } = Guid.NewGuid().ToString();

        internal Task<IList<BlobBatchSubOperationResponse>> ExecuteAsync(CloudBlobClient client, BlobRequestOptions requestOptions = default(BlobRequestOptions),
            OperationContext operationContext = default(OperationContext), CancellationToken cancellationToken = default(CancellationToken))
        {
            requestOptions = requestOptions ?? BlobRequestOptions.BaseDefaultRequestOptions;
            BlobRequestOptions modifiedOptions = BlobRequestOptions.ApplyDefaults(requestOptions, BlobType.Unspecified, client);

            return Executor.ExecuteAsync(
                BatchImpl(this, client, requestOptions),
                modifiedOptions.RetryPolicy,
                operationContext,
                cancellationToken);
        }

        private static RESTCommand<IList<BlobBatchSubOperationResponse>> BatchImpl(BatchOperation batch, CloudBlobClient client, BlobRequestOptions requestOptions)
        {
            // ContentMD5??
            //string contentMD5 = null;

            /*if (requestOptions.UseTransactionalMD5.HasValue && requestOptions.UseTransactionalMD5.Value)
            {
                contentMD5 = memoryStream.ComputeMD5Hash();
            }*/

            RESTCommand<IList<BlobBatchSubOperationResponse>> batchCmd = new RESTCommand<IList<BlobBatchSubOperationResponse>>(client.Credentials, client.StorageUri, client.HttpClient);
            requestOptions.ApplyToStorageCommand(batchCmd);

            List<BlobBatchSubOperationResponse> results = new List<BlobBatchSubOperationResponse>();

            batchCmd.CommandLocationMode = CommandLocationMode.PrimaryOnly;
            batchCmd.RetrieveResponseStream = true;
            batchCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.Accepted, resp, null /* retVal */, cmd, ex);
            batchCmd.BuildContent = (cmd, ctx) => BlobHttpRequestMessageFactory.WriteBatchBody(client, cmd, batch, ctx);
            batchCmd.BuildRequest = (cmd, uri, builder, cnt, serverTimeout, ctx) => BlobHttpRequestMessageFactory.PrepareBatchRequest(uri, client.BufferManager, serverTimeout, batch, cnt, ctx, client.GetCanonicalizer(), client.Credentials);
            batchCmd.PostProcessResponseAsync = (cmd, resp, ctx, ct) => BlobHttpResponseParsers.BatchPostProcessAsync(results, cmd, new[] { HttpStatusCode.Accepted, HttpStatusCode.OK }, resp, ctx, ct);

            return batchCmd;
        }

    }

    /// <summary>
    /// Represents the response to a single sub operation.
    /// </summary>
    public sealed class BlobBatchSubOperationResponse
    {
        /// <summary>
        /// Indicates the index in the list of sub-responses.
        /// </summary>
        public int OperationIndex { get; internal set; }

        /// <summary>
        /// The HTTP status code. 
        /// </summary>
        public HttpStatusCode StatusCode { get; internal set; }

        /// <summary>
        /// Any headers returned on the sub response.
        /// </summary>
        public Dictionary<string, string> Headers { get; internal set; }

        // TODO: add a field that holds the corresponding subRequest?

        /// <summary>
        /// Constructs a new sub response.
        /// </summary>
        internal BlobBatchSubOperationResponse()
        {
            Headers = new Dictionary<string, string>();
        }
    }

    /// <summary>
    /// Represents a failed response on a single sub response.
    /// </summary>
    public sealed class BlobBatchSubOperationError
    {
        /// <summary>
        /// Indicates the index in the list of sub-responses.
        /// </summary>
        public int OperationIndex { get; internal set; }

        /// <summary>
        /// The HTTP status code.
        /// </summary>
        public HttpStatusCode StatusCode { get; internal set; }

        /// <summary>
        /// The error code returned in the headers by the service. 
        /// </summary>
        public string ErrorCode { get; internal set; }

        /// <summary>
        /// The <see cref="StorageExtendedErrorInformation"/>. May be null.
        /// </summary>
        public StorageExtendedErrorInformation ExtendedErrorInformation { get; internal set; }

        internal BlobBatchSubOperationError() { }
    }

    /// <summary>
    /// This exception type is thrown when the uber request on a batch is successful but one or more of the sub requests has failed. 
    /// </summary>
    public class BlobBatchException : StorageException
    {
        /// <summary>
        /// The list of successful responses. 
        /// </summary>
        public IList<BlobBatchSubOperationResponse> SuccessfulResponses { get; internal set; }

        /// <summary>
        /// The list of failed responses. 
        /// </summary>
        public IList<BlobBatchSubOperationError> ErrorResponses { get; internal set; }

        internal BlobBatchException() : base(SR.BatchSubOperationError) { }
    }
}