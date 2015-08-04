//-----------------------------------------------------------------------
// <copyright file="ListSharesResponse.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Provides methods for parsing the response from a share listing operation.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
        sealed class ListSharesResponse : ResponseParsingBase<FileShareEntry>
    {
        /// <summary>
        /// Stores the share prefix.
        /// </summary>
        private string prefix;

        /// <summary>
        /// Signals when the share prefix can be consumed.
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
        /// Initializes a new instance of the <see cref="ListSharesResponse"/> class.
        /// </summary>
        /// <param name="stream">The stream to be parsed.</param>
        public ListSharesResponse(Stream stream)
            : base(stream)
        {
        }

        /// <summary>
        /// Gets the listing context from the XML response.
        /// </summary>
        /// <value>A set of parameters for the listing operation.</value>
        public ListingContext ListingContext
        {
            get
            {
                // Force a parsing in order
                ListingContext listingContext = new ListingContext(this.Prefix, this.MaxResults);
                listingContext.Marker = this.NextMarker;
                return listingContext;
            }
        }

        /// <summary>
        /// Gets an enumerable collection of <see cref="FileShareEntry"/> objects from the response.
        /// </summary>
        /// <value>An enumerable collection of <see cref="FileShareEntry"/> objects.</value>
        public IEnumerable<FileShareEntry> Shares
        {
            get
            {
                return this.ObjectsToParse;
            }
        }

        /// <summary>
        /// Gets the Prefix value provided for the listing operation from the XML response.
        /// </summary>
        /// <value>The Prefix value.</value>
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
        /// <value>The Marker value.</value>
        public string Marker
        {
            get
            {
                this.Variable(ref this.markerConsumable);

                return this.marker;
            }
        }

        /// <summary>
        /// Gets the MaxResults value provided for the listing operation from the XML response.
        /// </summary>
        /// <value>The MaxResults value.</value>
        public int MaxResults
        {
            get
            {
                this.Variable(ref this.maxResultsConsumable);

                return this.maxResults;
            }
        }

        /// <summary>
        /// Gets the NextMarker value from the XML response, if the listing was not complete.
        /// </summary>
        /// <value>The NextMarker value.</value>
        public string NextMarker
        {
            get
            {
                this.Variable(ref this.nextMarkerConsumable);

                return this.nextMarker;
            }
        }

        /// <summary>
        /// Reads a share entry completely including its properties and metadata.
        /// </summary>
        /// <returns>Share listing entry</returns>
        private FileShareEntry ParseShareEntry(Uri baseUri)
        {
            string name = null;
            IDictionary<string, string> metadata = null;
            FileShareProperties shareProperties = new FileShareProperties();

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
                            name = this.reader.ReadElementContentAsString();
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
                                            shareProperties.LastModified = reader.ReadElementContentAsString().ToUTCTime();
                                            break;

                                        case Constants.EtagElement:
                                            shareProperties.ETag = reader.ReadElementContentAsString();
                                            break;

                                        case Constants.QuotaElement:
                                            shareProperties.Quota = reader.ReadElementContentAsInt();
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
                            metadata = Response.ParseMetadata(this.reader);
                            break;

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            this.reader.ReadEndElement();

            if (metadata == null)
            {
                metadata = new Dictionary<string, string>();
            }

            return new FileShareEntry
            {
                Properties = shareProperties,
                Name = name,
                Uri = NavigationHelper.AppendPathToSingleUri(baseUri, name),
                Metadata = metadata,
            };
        }

        /// <summary>
        /// Parses the response XML for a share listing operation.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="FileShareEntry"/> objects.</returns>
        protected override IEnumerable<FileShareEntry> ParseXml()
        {
            if (this.reader.ReadToFollowing(Constants.EnumerationResultsElement))
            {
                if (this.reader.IsEmptyElement)
                {
                    this.reader.Skip();
                }
                else
                {
                    Uri baseUri = new Uri(this.reader.GetAttribute(Constants.ServiceEndpointElement));

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
                                case Constants.MarkerElement:
                                    this.marker = this.reader.ReadElementContentAsString();
                                    this.markerConsumable = true;
                                    yield return null;
                                    break;

                                case Constants.NextMarkerElement:
                                    this.nextMarker = this.reader.ReadElementContentAsString();
                                    this.nextMarkerConsumable = true;
                                    yield return null;
                                    break;

                                case Constants.MaxResultsElement:
                                    this.maxResults = this.reader.ReadElementContentAsInt();
                                    this.maxResultsConsumable = true;
                                    yield return null;
                                    break;

                                case Constants.PrefixElement:
                                    this.prefix = this.reader.ReadElementContentAsString();
                                    this.prefixConsumable = true;
                                    yield return null;
                                    break;

                                case Constants.SharesElement:
                                    this.reader.ReadStartElement();
                                    while (this.reader.IsStartElement(Constants.ShareElement))
                                    {
                                        yield return this.ParseShareEntry(baseUri);
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
