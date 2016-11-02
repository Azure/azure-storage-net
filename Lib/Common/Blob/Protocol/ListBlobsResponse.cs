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

namespace Microsoft.WindowsAzure.Storage.Blob.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// Provides methods for parsing the response from a blob listing operation.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
 sealed class ListBlobsResponse : ResponseParsingBase<IListBlobEntry>
    {
        /// <summary>
        /// Stores the blob prefix.
        /// </summary>
        private string prefix;

        /// <summary>
        /// Signals when the blob prefix can be consumed.
        /// </summary>
        private bool prefixConsumable;

        /// <summary>
        /// Stores the marker.
        /// </summary>
        private string marker;

        /// <summary>
        /// Signals when the marker can be consumed.
        /// </summary>
        private bool markerConsumable;

        /// <summary>
        /// Stores the blob delimiter.
        /// </summary>
        private string delimiter;

        /// <summary>
        /// Signals when the blob delimiter can be consumed.
        /// </summary>
        private bool delimiterConsumable;

        /// <summary>
        /// Stores the max results.
        /// </summary>
        private int maxResults;

        /// <summary>
        /// Signals when the max results can be consumed.
        /// </summary>
        private bool maxResultsConsumable;

        /// <summary>
        /// Stores the next marker.
        /// </summary>
        private string nextMarker;

        /// <summary>
        /// Signals when the next marker can be consumed.
        /// </summary>
        private bool nextMarkerConsumable;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListBlobsResponse"/> class.
        /// </summary>
        /// <param name="stream">The stream to be parsed.</param>
        public ListBlobsResponse(Stream stream)
            : base(stream)
        {
        }

        /// <summary>
        /// Gets the listing context from the XML response.
        /// </summary>
        /// <value>A <see cref="BlobListingContext"/> object.</value>
        public BlobListingContext ListingContext
        {
            get
            {
                BlobListingContext listingContext = new BlobListingContext(this.Prefix, this.MaxResults, this.Delimiter, BlobListingDetails.None);
                listingContext.Marker = this.NextMarker;
                return listingContext;
            }
        }

        /// <summary>
        /// Gets an enumerable collection of objects that implement <see cref="IListBlobEntry"/> from the response.
        /// </summary>
        /// <value>An enumerable collection of objects that implement <see cref="IListBlobEntry"/>.</value>
        public IEnumerable<IListBlobEntry> Blobs
        {
            get
            {
                return this.ObjectsToParse;
            }
        }

        /// <summary>
        /// Gets the Prefix value provided for the listing operation from the XML response.
        /// </summary>
        /// <value>A string containing the Prefix value.</value>
        public string Prefix
        {
            get
            {
                this.Variable(ref this.prefixConsumable);

                return this.prefix;
            }
        }

        /// <summary>
        /// Gets the Marker value provided for the listing operation from the XML response.
        /// </summary>
        /// <value>A string containing the Marker value.</value>
        public string Marker
        {
            get
            {
                this.Variable(ref this.markerConsumable);

                return this.marker;
            }
        }

        /// <summary>
        /// Gets the Delimiter value provided for the listing operation from the XML response.
        /// </summary>
        /// <value>A string containing the Delimiter value.</value>
        public string Delimiter
        {
            get
            {
                this.Variable(ref this.delimiterConsumable);

                return this.delimiter;
            }
        }

        /// <summary>
        /// Gets the MaxResults value provided for the listing operation from the XML response.
        /// </summary>
        /// <value>An integer containing the MaxResults value.</value>
        public int MaxResults
        {
            get
            {
                this.Variable(ref this.maxResultsConsumable);

                return this.maxResults;
            }
        }

        /// <summary>
        /// Gets or sets the NextMarker value from the XML response, if the listing was not complete.
        /// </summary>
        /// <value>A string containing the NextMarker value.</value>
        public string NextMarker
        {
            get
            {
                this.Variable(ref this.nextMarkerConsumable);

                return this.nextMarker;
            }
        }

        /// <summary>
        /// Parses a blob entry in a blob listing response.
        /// </summary>
        /// <returns>Blob listing entry</returns>
        [SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity", Justification = "Reviewed.")]
        private IListBlobEntry ParseBlobEntry(Uri baseUri)
        {
            BlobAttributes blob = new BlobAttributes();
            string name = null;

            // copy blob attribute strings
            string copyId = null;
            string copyStatus = null;
            string copyCompletionTime = null;
            string copyProgress = null;
            string copySource = null;
            string copyStatusDescription = null;

            this.reader.ReadStartElement();
            while (this.reader.IsStartElement())
            {
                if (this.reader.IsEmptyElement)
                {
                    this.reader.Skip();
                }
                else
                {
                    switch (this.reader.Name)
                    {
                        case Constants.NameElement:
                            name = reader.ReadElementContentAsString();
                            break;

                        case Constants.SnapshotElement:
                            blob.SnapshotTime = reader.ReadElementContentAsString().ToUTCTime();
                            break;

                        case Constants.PropertiesElement:
                            this.reader.ReadStartElement();
                            while (this.reader.IsStartElement())
                            {
                                if (this.reader.IsEmptyElement)
                                {
                                    this.reader.Skip();
                                }
                                else
                                {
                                    switch (this.reader.Name)
                                    {
                                        case Constants.LastModifiedElement:
                                            blob.Properties.LastModified = reader.ReadElementContentAsString().ToUTCTime();
                                            break;

                                        case Constants.EtagElement:
                                            blob.Properties.ETag = string.Format(CultureInfo.InvariantCulture, "\"{0}\"", reader.ReadElementContentAsString());
                                            break;

                                        case Constants.ContentLengthElement:
                                            blob.Properties.Length = reader.ReadElementContentAsLong();
                                            break;

                                        case Constants.CacheControlElement:
                                            blob.Properties.CacheControl = reader.ReadElementContentAsString();
                                            break;

                                        case Constants.ContentTypeElement:
                                            blob.Properties.ContentType = reader.ReadElementContentAsString();
                                            break;

                                        case Constants.HeaderConstants.ContentDispositionResponseHeader:
                                            blob.Properties.ContentDisposition = reader.ReadElementContentAsString();
                                            break;

                                        case Constants.ContentEncodingElement:
                                            blob.Properties.ContentEncoding = reader.ReadElementContentAsString();
                                            break;

                                        case Constants.ContentLanguageElement:
                                            blob.Properties.ContentLanguage = reader.ReadElementContentAsString();
                                            break;

                                        case Constants.ContentMD5Element:
                                            blob.Properties.ContentMD5 = reader.ReadElementContentAsString();
                                            break;

                                        case Constants.BlobTypeElement:
                                            string blobTypeString = reader.ReadElementContentAsString();
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
                                            blob.Properties.LeaseStatus = BlobHttpResponseParsers.GetLeaseStatus(reader.ReadElementContentAsString());
                                            break;

                                        case Constants.LeaseStateElement:
                                            blob.Properties.LeaseState = BlobHttpResponseParsers.GetLeaseState(reader.ReadElementContentAsString());
                                            break;

                                        case Constants.LeaseDurationElement:
                                            blob.Properties.LeaseDuration = BlobHttpResponseParsers.GetLeaseDuration(reader.ReadElementContentAsString());
                                            break;

                                        case Constants.CopyIdElement:
                                            copyId = reader.ReadElementContentAsString();
                                            break;

                                        case Constants.CopyCompletionTimeElement:
                                            copyCompletionTime = reader.ReadElementContentAsString();
                                            break;

                                        case Constants.CopyStatusElement:
                                            copyStatus = reader.ReadElementContentAsString();
                                            break;

                                        case Constants.CopyProgressElement:
                                            copyProgress = reader.ReadElementContentAsString();
                                            break;

                                        case Constants.CopySourceElement:
                                            copySource = reader.ReadElementContentAsString();
                                            break;

                                        case Constants.CopyStatusDescriptionElement:
                                            copyStatusDescription = reader.ReadElementContentAsString();
                                            break;

                                        case Constants.ServerEncryptionElement:
                                            blob.Properties.IsServerEncrypted = BlobHttpResponseParsers.GetServerEncrypted(reader.ReadElementContentAsString());
                                            break;

                                        default:
                                            reader.Skip();
                                            break;
                                    }
                                }
                            }

                            this.reader.ReadEndElement();
                            break;

                        case Constants.MetadataElement:
                            blob.Metadata = Response.ParseMetadata(this.reader);
                            break;

                        default:
                            this.reader.Skip();
                            break;
                    }
                }
            }

            this.reader.ReadEndElement();

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
                    copyStatusDescription);
            }

            return new ListBlobEntry(name, blob);
        }

        /// <summary>
        /// Parses a blob prefix entry in a blob listing response.
        /// </summary>
        /// <returns>Blob listing entry</returns>
        private IListBlobEntry ParseBlobPrefixEntry()
        {
            ListBlobPrefixEntry commonPrefix = new ListBlobPrefixEntry();

            this.reader.ReadStartElement();
            while (this.reader.IsStartElement())
            {
                if (this.reader.IsEmptyElement)
                {
                    this.reader.Skip();
                }
                else
                {
                    switch (this.reader.Name)
                    {
                        case Constants.NameElement:
                            commonPrefix.Name = reader.ReadElementContentAsString();
                            break;

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            this.reader.ReadEndElement();

            return commonPrefix;
        }

        /// <summary>
        /// Parses the response XML for a blob listing operation.
        /// </summary>
        /// <returns>An enumerable collection of objects that implement <see cref="IListBlobEntry"/>.</returns>
        protected override IEnumerable<IListBlobEntry> ParseXml()
        {
            if (this.reader.ReadToFollowing(Constants.EnumerationResultsElement))
            {
                if (this.reader.IsEmptyElement)
                {
                    this.reader.Skip();
                }
                else
                {
                    Uri baseUri;
                    string serviceEndpoint = this.reader.GetAttribute(Constants.ServiceEndpointElement);
                    if (!string.IsNullOrEmpty(serviceEndpoint))
                    {
                        baseUri = NavigationHelper.AppendPathToSingleUri(
                                new Uri(serviceEndpoint),
                                this.reader.GetAttribute(Constants.ContainerNameElement));
                    }
                    else
                    {
                        baseUri = new Uri(this.reader.GetAttribute(Constants.ContainerNameElement));
                    }

                    this.reader.ReadStartElement();
                    while (this.reader.IsStartElement())
                    {
                        if (this.reader.IsEmptyElement)
                        {
                            this.reader.Skip();
                        }
                        else
                        {
                            switch (this.reader.Name)
                            {
                                case Constants.DelimiterElement:
                                    this.delimiter = reader.ReadElementContentAsString();
                                    this.delimiterConsumable = true;
                                    yield return null;
                                    break;

                                case Constants.MarkerElement:
                                    this.marker = reader.ReadElementContentAsString();
                                    this.markerConsumable = true;
                                    yield return null;
                                    break;

                                case Constants.NextMarkerElement:
                                    this.nextMarker = reader.ReadElementContentAsString();
                                    this.nextMarkerConsumable = true;
                                    yield return null;
                                    break;

                                case Constants.MaxResultsElement:
                                    this.maxResults = reader.ReadElementContentAsInt();
                                    this.maxResultsConsumable = true;
                                    yield return null;
                                    break;

                                case Constants.PrefixElement:
                                    this.prefix = reader.ReadElementContentAsString();
                                    this.prefixConsumable = true;
                                    yield return null;
                                    break;

                                case Constants.BlobsElement:
                                    this.reader.ReadStartElement();
                                    while (this.reader.IsStartElement())
                                    {
                                        switch (this.reader.Name)
                                        {
                                            case Constants.BlobElement:
                                                yield return this.ParseBlobEntry(baseUri);
                                                break;

                                            case Constants.BlobPrefixElement:
                                                yield return this.ParseBlobPrefixEntry();
                                                break;
                                        }
                                    }

                                    this.reader.ReadEndElement();
                                    this.allObjectsParsed = true;
                                    break;

                                default:
                                    reader.Skip();
                                    break;
                            }
                        }
                    }

                    this.reader.ReadEndElement();
                }
            }
        }
    }
}
