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
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Provides methods for parsing the response from a file listing operation.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
        sealed class ListFilesAndDirectoriesResponse
    {
        /// <summary>
        /// Gets an enumerable collection of objects that implement <see cref="IListFileEntry"/> from the response.
        /// </summary>
        /// <value>An enumerable collection of objects that implement <see cref="IListFileEntry"/>.</value>
        public IEnumerable<IListFileEntry> Files { get; private set; }

        /// <summary>
        /// Gets the NextMarker value from the XML response, if the listing was not complete.
        /// </summary>
        /// <value>The NextMarker value.</value>
        public string NextMarker { get; private set; }
        
        private ListFilesAndDirectoriesResponse()
        {
        }

        /// <summary>
        /// Parses a file entry in a file listing response.
        /// </summary>
        /// <returns>File listing entry</returns>
        private static async Task<IListFileEntry> ParseFileEntryAsync(XmlReader reader, Uri baseUri, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            CloudFileAttributes file = new CloudFileAttributes();
            string name = null;

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
                                        case Constants.ContentLengthElement:
                                            file.Properties.Length = await reader.ReadElementContentAsInt64Async().ConfigureAwait(false);
                                            break;

                                        default:
                                            await reader.SkipAsync().ConfigureAwait(false);
                                            break;
                                    }
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

            Uri uri = NavigationHelper.AppendPathToSingleUri(baseUri, name);
            file.StorageUri = new StorageUri(uri);

            return new ListFileEntry(name, file);
        }

        /// <summary>
        /// Parses a file directory entry in a file listing response.
        /// </summary>
        /// <returns>File listing entry</returns>
        private static async Task<IListFileEntry> ParseFileDirectoryEntryAsync(XmlReader reader, Uri baseUri, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            FileDirectoryProperties properties = new FileDirectoryProperties();
            string name = null;

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
                                            properties.LastModified = (await reader.ReadElementContentAsStringAsync().ConfigureAwait(false)).ToUTCTime();
                                            break;

                                        case Constants.EtagElement:
                                            properties.ETag = string.Format(CultureInfo.InvariantCulture, "\"{0}\"", await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                                            break;

                                        default:
                                            await reader.SkipAsync().ConfigureAwait(false);
                                            break;
                                    }
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

            Uri uri = NavigationHelper.AppendPathToSingleUri(baseUri, name);

            return new ListFileDirectoryEntry(name, uri, properties);
        }

        /// <summary>
        /// Parses the response XML for a file listing operation.
        /// </summary>
        /// <returns>An enumerable collection of objects that implement <see cref="IListFileEntry"/>.</returns>
        internal static async Task<ListFilesAndDirectoriesResponse> ParseAsync(Stream stream, CancellationToken token)
        {
            using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(stream))
            {
                token.ThrowIfCancellationRequested();

                List<IListFileEntry> entries = new List<IListFileEntry>();
                string nextMarker = default(string);

                if (await reader.ReadToFollowingAsync(Constants.EnumerationResultsElement).ConfigureAwait(false))
                {
                    if (reader.IsEmptyElement)
                    {
                        await reader.SkipAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        Uri baseUri = new Uri(reader.GetAttribute(Constants.ServiceEndpointElement));
                        baseUri = NavigationHelper.AppendPathToSingleUri(baseUri, reader.GetAttribute(Constants.ShareNameElement));
                        baseUri = NavigationHelper.AppendPathToSingleUri(baseUri, reader.GetAttribute(Constants.DirectoryPathElement));

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
                                    case Constants.MarkerElement:
                                        await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                        break;

                                    case Constants.NextMarkerElement:
                                        nextMarker = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                        break;

                                    case Constants.MaxResultsElement:
                                        await reader.ReadElementContentAsInt32Async().ConfigureAwait(false);
                                        break;

                                    case Constants.EntriesElement:
                                        await reader.ReadStartElementAsync().ConfigureAwait(false);
                                        while (await reader.IsStartElementAsync().ConfigureAwait(false))
                                        {
                                            switch (reader.Name)
                                            {
                                                case Constants.FileElement:
                                                    entries.Add(await ParseFileEntryAsync(reader, baseUri, token).ConfigureAwait(false));
                                                    break;

                                                case Constants.FileDirectoryElement:
                                                    entries.Add(await ParseFileDirectoryEntryAsync(reader, baseUri, token).ConfigureAwait(false));
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

                return new ListFilesAndDirectoriesResponse { Files = entries, NextMarker = nextMarker };
            }
        }
    }
}
