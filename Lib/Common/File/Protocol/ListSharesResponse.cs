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
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Provides methods for parsing the response from a share listing operation.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
        sealed class ListSharesResponse
    {
        /// <summary>
        /// Gets an enumerable collection of <see cref="FileShareEntry"/> objects from the response.
        /// </summary>
        /// <value>An enumerable collection of <see cref="FileShareEntry"/> objects.</value>
        public IEnumerable<FileShareEntry> Shares { get; private set; }

        /// <summary>
        /// Gets the NextMarker value from the XML response, if the listing was not complete.
        /// </summary>
        /// <value>The NextMarker value.</value>
        public string NextMarker { get; private set; }

        /// <summary>
        /// Reads a share entry completely including its properties and metadata.
        /// </summary>
        /// <returns>Share listing entry</returns>
        private static async Task<FileShareEntry> ParseShareEntryAsync(XmlReader reader, Uri baseUri, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            string name = null;
            DateTimeOffset? snapshotTime = null;
            IDictionary<string, string> metadata = null;
            FileShareProperties shareProperties = new FileShareProperties();

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
                            snapshotTime = (await reader.ReadElementContentAsStringAsync().ConfigureAwait(false)).ToUTCTime();
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
                                            shareProperties.LastModified = (await reader.ReadElementContentAsStringAsync().ConfigureAwait(false)).ToUTCTime();
                                            break;

                                        case Constants.EtagElement:
                                            shareProperties.ETag = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.QuotaElement:
                                            shareProperties.Quota = await reader.ReadElementContentAsInt32Async().ConfigureAwait(false);
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
                            metadata = await Response.ParseMetadataAsync(reader).ConfigureAwait(false);
                            break;

                        default:
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
            }

            await reader.ReadEndElementAsync().ConfigureAwait(false);

            return new FileShareEntry
            {
                Properties = shareProperties,
                Name = name,
                Uri = NavigationHelper.AppendPathToSingleUri(baseUri, name),
                Metadata = metadata ?? new Dictionary<string, string>(),
                SnapshotTime = snapshotTime,
            };
        }

        /// <summary>
        /// Parses the response XML for a share listing operation.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="FileShareEntry"/> objects.</returns>
        internal static async Task<ListSharesResponse> ParseAsync(Stream stream, CancellationToken token)
        {
            using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(stream))
            {
                token.ThrowIfCancellationRequested();

                List<FileShareEntry> shares = new List<FileShareEntry>();
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

                                    case Constants.PrefixElement:
                                        await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                        break;

                                    case Constants.SharesElement:
                                        await reader.ReadStartElementAsync().ConfigureAwait(false);
                                        while (await reader.IsStartElementAsync(Constants.ShareElement))
                                        {
                                            shares.Add(await ParseShareEntryAsync(reader, baseUri, token).ConfigureAwait(false));
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

                return new ListSharesResponse { Shares = shares, NextMarker = nextMarker };
            }
        }
    }
}
