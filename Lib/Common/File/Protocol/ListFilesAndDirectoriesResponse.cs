//-----------------------------------------------------------------------
// <copyright file="ListFilesAndDirectoriesResponse.cs" company="Microsoft">
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
    using System.Globalization;
    using System.IO;

    /// <summary>
    /// Provides methods for parsing the response from a file listing operation.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
        sealed class ListFilesAndDirectoriesResponse : ResponseParsingBase<IListFileEntry>
    {
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
        /// Initializes a new instance of the <see cref="ListFilesAndDirectoriesResponse"/> class.
        /// </summary>
        /// <param name="stream">The stream to be parsed.</param>
        public ListFilesAndDirectoriesResponse(Stream stream)
            : base(stream)
        {
        }

        /// <summary>
        /// Gets the listing context from the XML response.
        /// </summary>
        /// <value>A set of parameters for the listing operation.</value>
        public FileListingContext ListingContext
        {
            get
            {
                FileListingContext listingContext = new FileListingContext(this.MaxResults);
                listingContext.Marker = this.NextMarker;
                return listingContext;
            }
        }

        /// <summary>
        /// Gets an enumerable collection of objects that implement <see cref="IListFileEntry"/> from the response.
        /// </summary>
        /// <value>An enumerable collection of objects that implement <see cref="IListFileEntry"/>.</value>
        public IEnumerable<IListFileEntry> Files
        {
            get
            {
                return this.ObjectsToParse;
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
        /// Parses a file entry in a file listing response.
        /// </summary>
        /// <returns>File listing entry</returns>
        private IListFileEntry ParseFileEntry(Uri baseUri)
        {
            CloudFileAttributes file = new CloudFileAttributes();
            string name = null;

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
                                        case Constants.ContentLengthElement:
                                            file.Properties.Length = reader.ReadElementContentAsLong();
                                            break;

                                        default:
                                            this.reader.Skip();
                                            break;
                                    }
                                }
                            }

                            this.reader.ReadEndElement();
                            break;

                        default:
                            this.reader.Skip();
                            break;
                    }
                }
            }

            this.reader.ReadEndElement();

            Uri uri = NavigationHelper.AppendPathToSingleUri(baseUri, name);
            file.StorageUri = new StorageUri(uri);

            return new ListFileEntry(name, file);
        }

        /// <summary>
        /// Parses a file directory entry in a file listing response.
        /// </summary>
        /// <returns>File listing entry</returns>
        private IListFileEntry ParseFileDirectoryEntry(Uri baseUri)
        {
            FileDirectoryProperties properties = new FileDirectoryProperties();
            string name = null;

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
                                            properties.LastModified = reader.ReadElementContentAsString().ToUTCTime();
                                            break;

                                        case Constants.EtagElement:
                                            properties.ETag = string.Format(CultureInfo.InvariantCulture, "\"{0}\"", reader.ReadElementContentAsString());
                                            break;

                                        default:
                                            this.reader.Skip();
                                            break;
                                    }
                                }
                            }

                            this.reader.ReadEndElement();
                            break;

                        default:
                            this.reader.Skip();
                            break;
                    }
                }
            }

            this.reader.ReadEndElement();

            Uri uri = NavigationHelper.AppendPathToSingleUri(baseUri, name);

            return new ListFileDirectoryEntry(name, uri, properties);
        }

        /// <summary>
        /// Parses the response XML for a file listing operation.
        /// </summary>
        /// <returns>An enumerable collection of objects that implement <see cref="IListFileEntry"/>.</returns>
        protected override IEnumerable<IListFileEntry> ParseXml()
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
                    baseUri = NavigationHelper.AppendPathToSingleUri(baseUri, this.reader.GetAttribute(Constants.ShareNameElement));
                    baseUri = NavigationHelper.AppendPathToSingleUri(baseUri, this.reader.GetAttribute(Constants.DirectoryPathElement));

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

                                case Constants.EntriesElement:
                                    this.reader.ReadStartElement();
                                    while (this.reader.IsStartElement())
                                    {
                                        switch (this.reader.Name)
                                        {
                                            case Constants.FileElement:
                                                yield return this.ParseFileEntry(baseUri);
                                                break;

                                            case Constants.FileDirectoryElement:
                                                yield return this.ParseFileDirectoryEntry(baseUri);
                                                break;
                                        }
                                    }

                                    this.reader.ReadEndElement();
                                    this.allObjectsParsed = true;
                                    break;

                                default:
                                    this.reader.Skip();
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
