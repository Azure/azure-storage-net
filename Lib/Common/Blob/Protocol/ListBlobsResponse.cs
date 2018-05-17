//-----------------------------------------------------------------------
// <copyright file="ListBlobsResponse.cs" company="Microsoft">
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
//-----------------------------------------------------------------------

namespace Microsoft.Azure.Storage.Blob.Protocol
{
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Provides methods for parsing the response from a blob listing operation.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
 sealed class ListBlobsResponse 
    {
        /// <summary>
        /// Gets an enumerable collection of objects that implement <see cref="IListBlobEntry"/> from the response.
        /// </summary>
        /// <value>An enumerable collection of objects that implement <see cref="IListBlobEntry"/>.</value>
        public IEnumerable<IListBlobEntry> Blobs { get; private set; }


        public string NextMarker { get; private set; }

        private ListBlobsResponse()
        {
        }

        /// <summary>
        /// Parses a blob entry in a blob listing response.
        /// </summary>
        /// <returns>Blob listing entry</returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Reviewed.")]
        private static async Task<IListBlobEntry> ParseBlobEntryAsync(XmlReader reader, Uri baseUri, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            BlobAttributes blob = new BlobAttributes();
            string name = null;

            // copy blob attribute strings
            string copyId = null;
            string copyStatus = null;
            string copyCompletionTime = null;
            string copyProgress = null;
            string copySource = null;
            string copyStatusDescription = null;
            string copyDestinationSnapshotTime = null;

            string blobTierString = null;
            bool? blobTierInferred = null;
            string rehydrationStatusString = null;
            DateTimeOffset? blobTierLastModifiedTime = null;

            await reader.ReadStartElementAsync().ConfigureAwait(false);
            while (await reader.IsStartElementAsync().ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();

                if (reader.IsEmptyElement)
                {
                    await reader.SkipAsync().ConfigureAwait(false);
                }
                else
                {
                    switch (reader.Name)
                    {
                        case Constants.NameElement:
                            name = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            break;

                        case Constants.SnapshotElement:
                            blob.SnapshotTime = (await reader.ReadElementContentAsStringAsync().ConfigureAwait(false)).ToUTCTime();
                            break;

                        case Constants.DeletedElement:
                            blob.IsDeleted = BlobHttpResponseParsers.GetDeletionStatus(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                            break;

                        case Constants.PropertiesElement:
                            await reader.ReadStartElementAsync().ConfigureAwait(false);
                            while (await reader.IsStartElementAsync().ConfigureAwait(false))
                            {
                                token.ThrowIfCancellationRequested();

                                if (reader.IsEmptyElement)
                                {
                                    await reader.SkipAsync().ConfigureAwait(false);
                                }
                                else
                                {
                                    switch (reader.Name)
                                    {
                                        case Constants.LastModifiedElement:
                                            blob.Properties.LastModified = (await reader.ReadElementContentAsStringAsync().ConfigureAwait(false)).ToUTCTime();
                                            break;

                                        case Constants.EtagElement:
                                            blob.Properties.ETag = string.Format(CultureInfo.InvariantCulture, "\"{0}\"", await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                                            break;

                                        case Constants.ContentLengthElement:
                                            blob.Properties.Length = await reader.ReadElementContentAsInt64Async().ConfigureAwait(false);
                                            break;

                                        case Constants.CacheControlElement:
                                            blob.Properties.CacheControl = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.ContentTypeElement:
                                            blob.Properties.ContentType = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.HeaderConstants.ContentDispositionResponseHeader:
                                            blob.Properties.ContentDisposition = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.ContentEncodingElement:
                                            blob.Properties.ContentEncoding = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.ContentLanguageElement:
                                            blob.Properties.ContentLanguage = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.ContentMD5Element:
                                            blob.Properties.ContentMD5 = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.BlobTypeElement:
                                            string blobTypeString = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            switch (blobTypeString)
                                            {
                                                case Constants.BlockBlobValue:
                                                    blob.Properties.BlobType = BlobType.BlockBlob;
                                                    break;

                                                case Constants.PageBlobValue:
                                                    blob.Properties.BlobType = BlobType.PageBlob;
                                                    break;

                                                case Constants.AppendBlobValue:
                                                    blob.Properties.BlobType = BlobType.AppendBlob;
                                                    break;
                                            }

                                            break;

                                        case Constants.LeaseStatusElement:
                                            blob.Properties.LeaseStatus = BlobHttpResponseParsers.GetLeaseStatus(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                                            break;

                                        case Constants.LeaseStateElement:
                                            blob.Properties.LeaseState = BlobHttpResponseParsers.GetLeaseState(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                                            break;

                                        case Constants.LeaseDurationElement:
                                            blob.Properties.LeaseDuration = BlobHttpResponseParsers.GetLeaseDuration(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                                            break;

                                        case Constants.CopyIdElement:
                                            copyId = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.CopyCompletionTimeElement:
                                            copyCompletionTime = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.CopyStatusElement:
                                            copyStatus = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.CopyProgressElement:
                                            copyProgress = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.CopySourceElement:
                                            copySource = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.CopyStatusDescriptionElement:
                                            copyStatusDescription = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.ServerEncryptionElement:
                                            blob.Properties.IsServerEncrypted = BlobHttpResponseParsers.GetServerEncrypted(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                                            break;

                                        case Constants.IncrementalCopy:
                                            blob.Properties.IsIncrementalCopy = BlobHttpResponseParsers.GetIncrementalCopyStatus(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                                            break;

                                        case Constants.CopyDestinationSnapshotElement:
                                            copyDestinationSnapshotTime = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.AccessTierElement:
                                            blobTierString = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.ArchiveStatusElement:
                                            rehydrationStatusString = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.AccessTierInferred:
                                            blobTierInferred = await reader.ReadElementContentAsBooleanAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.AccessTierChangeTimeElement:
                                            string t = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            blobTierLastModifiedTime = DateTimeOffset.Parse(t, CultureInfo.InvariantCulture);
                                            break;

                                        case Constants.DeletedTimeElement:
                                            blob.Properties.DeletedTime = (await reader.ReadElementContentAsStringAsync().ConfigureAwait(false)).ToUTCTime();
                                            break;

                                        case Constants.RemainingRetentionDaysElement:
                                            blob.Properties.RemainingDaysBeforePermanentDelete = int.Parse(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                                            break;

                                        default:
                                            await reader.SkipAsync().ConfigureAwait(false);
                                            break;
                                    }
                                }
                            }

                            await reader.ReadEndElementAsync().ConfigureAwait(false);
                            break;

                        case Constants.MetadataElement:
                            blob.Metadata = await Response.ParseMetadataAsync(reader).ConfigureAwait(false);
                            break;

                        default:
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
            }

            await reader.ReadEndElementAsync().ConfigureAwait(false);

            Uri uri = NavigationHelper.AppendPathToSingleUri(baseUri, name);

            if (blob.SnapshotTime.HasValue)
            {
                UriQueryBuilder builder = new UriQueryBuilder();
                builder.Add("snapshot", Request.ConvertDateTimeToSnapshotString(blob.SnapshotTime.Value));
                uri = builder.AddToUri(uri);
            }

            blob.StorageUri = new StorageUri(uri);

            if (!string.IsNullOrEmpty(copyStatus))
            {
                blob.CopyState = BlobHttpResponseParsers.GetCopyAttributes(
                    copyStatus,
                    copyId,
                    copySource,
                    copyProgress,
                    copyCompletionTime,
                    copyStatusDescription,
                    copyDestinationSnapshotTime);
            }

            if (!string.IsNullOrEmpty(blobTierString))
            {
                StandardBlobTier? standardBlobTier;
                PremiumPageBlobTier? premiumPageBlobTier;
                BlobHttpResponseParsers.GetBlobTier(blob.Properties.BlobType, blobTierString, out standardBlobTier, out premiumPageBlobTier);
                blob.Properties.StandardBlobTier = standardBlobTier;
                blob.Properties.PremiumPageBlobTier = premiumPageBlobTier;
            }

            blob.Properties.RehydrationStatus = BlobHttpResponseParsers.GetRehydrationStatus(rehydrationStatusString);
            blob.Properties.BlobTierLastModifiedTime = blobTierLastModifiedTime;
            blob.Properties.BlobTierInferred = blobTierInferred;

            return new ListBlobEntry(name, blob);
        }

        /// <summary>
        /// Parses a blob prefix entry in a blob listing response.
        /// </summary>
        /// <returns>Blob listing entry</returns>
        private static async Task<ListBlobPrefixEntry> ParseBlobPrefixEntryAsync(XmlReader reader, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            ListBlobPrefixEntry commonPrefix = new ListBlobPrefixEntry();

            await reader.ReadStartElementAsync().ConfigureAwait(false);
            while (await reader.IsStartElementAsync().ConfigureAwait(false))
            {
                token.ThrowIfCancellationRequested();

                if (reader.IsEmptyElement)
                {
                    await reader.SkipAsync().ConfigureAwait(false);
                }
                else
                {
                    switch (reader.Name)
                    {
                        case Constants.NameElement:
                            commonPrefix.Name = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            break;

                        default:
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
            }

            await reader.ReadEndElementAsync().ConfigureAwait(false);

            return commonPrefix;
        }

        /// <summary>
        /// Parses the response XML for a blob listing operation.
        /// </summary>
        /// <returns>An enumerable collection of objects that implement <see cref="IListBlobEntry"/>.</returns>
        internal static async Task<ListBlobsResponse> ParseAsync(Stream stream, CancellationToken token)
        {
            using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(stream))
            {
                token.ThrowIfCancellationRequested();

                List<IListBlobEntry> entries = new List<IListBlobEntry>();
                string nextMarker = default(string);

                if (await reader.ReadToFollowingAsync(Constants.EnumerationResultsElement).ConfigureAwait(false))
                {
                    if (reader.IsEmptyElement)
                    {
                        await reader.SkipAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        Uri baseUri;
                        string serviceEndpoint = reader.GetAttribute(Constants.ServiceEndpointElement);
                        if (!string.IsNullOrEmpty(serviceEndpoint))
                        {
                            baseUri = NavigationHelper.AppendPathToSingleUri(
                                    new Uri(serviceEndpoint),
                                    reader.GetAttribute(Constants.ContainerNameElement));
                        }
                        else
                        {
                            baseUri = new Uri(reader.GetAttribute(Constants.ContainerNameElement));
                        }

                        await reader.ReadStartElementAsync().ConfigureAwait(false);
                        while (await reader.IsStartElementAsync().ConfigureAwait(false))
                        {
                            token.ThrowIfCancellationRequested();

                            if (reader.IsEmptyElement)
                            {
                                await reader.SkipAsync().ConfigureAwait(false);
                            }
                            else
                            {
                                switch (reader.Name)
                                {
                                    case Constants.DelimiterElement:
                                        await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                        break;

                                    case Constants.MarkerElement:
                                        await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                        break;

                                    case Constants.NextMarkerElement:
                                        nextMarker = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                        break;

                                    case Constants.MaxResultsElement:
                                        await reader.ReadElementContentAsInt32Async().ConfigureAwait(false);
                                        break;

                                    case Constants.PrefixElement:
                                        await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                        break;

                                    case Constants.BlobsElement:
                                        await reader.ReadStartElementAsync().ConfigureAwait(false);
                                        while (await reader.IsStartElementAsync().ConfigureAwait(false))
                                        {
                                            switch (reader.Name)
                                            {
                                                case Constants.BlobElement:
                                                    entries.Add(await ParseBlobEntryAsync(reader, baseUri, token).ConfigureAwait(false));
                                                    break;

                                                case Constants.BlobPrefixElement:
                                                    entries.Add(await ParseBlobPrefixEntryAsync(reader, token).ConfigureAwait(false));
                                                    break;
                                            }
                                        }

                                        await reader.ReadEndElementAsync().ConfigureAwait(false);
                                        break;

                                    default:
                                        await reader.SkipAsync().ConfigureAwait(false);
                                        break;
                                }
                            }
                        }

                        await reader.ReadEndElementAsync().ConfigureAwait(false);
                    }
                }

                return new ListBlobsResponse { Blobs = entries, NextMarker = nextMarker };
            }
        }
    }
}
