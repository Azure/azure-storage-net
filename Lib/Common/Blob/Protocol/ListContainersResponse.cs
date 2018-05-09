//-----------------------------------------------------------------------
// <copyright file="ListContainersResponse.cs" company="Microsoft">
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
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Provides methods for parsing the response from a container listing operation.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
        sealed class ListContainersResponse
    {
        public string NextMarker { get; private set; }
        public IEnumerable<BlobContainerEntry> Containers { get; private set; }

        private ListContainersResponse()
        {
        }

        /// <summary>
        /// Reads a container entry completely including its properties and metadata.
        /// </summary>
        /// <returns>Container listing entry</returns>
        private static async Task<BlobContainerEntry> ParseContainerEntryAsync(XmlReader reader, Uri baseUri, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            string name = null;
            IDictionary<string, string> metadata = null;
            BlobContainerProperties containerProperties = new BlobContainerProperties();
            containerProperties.PublicAccess = BlobContainerPublicAccessType.Off;

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
                                            containerProperties.LastModified = (await reader.ReadElementContentAsStringAsync().ConfigureAwait(false)).ToUTCTime();
                                            break;

                                        case Constants.EtagElement:
                                            containerProperties.ETag = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                                            break;

                                        case Constants.LeaseStatusElement:
                                            containerProperties.LeaseStatus = BlobHttpResponseParsers.GetLeaseStatus(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                                            break;

                                        case Constants.LeaseStateElement:
                                            containerProperties.LeaseState = BlobHttpResponseParsers.GetLeaseState(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                                            break;

                                        case Constants.LeaseDurationElement:
                                            containerProperties.LeaseDuration = BlobHttpResponseParsers.GetLeaseDuration(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                                            break;

                                        case Constants.PublicAccessElement:
                                            containerProperties.PublicAccess = ContainerHttpResponseParsers.GetContainerAcl(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
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

            if (metadata == null)
            {
                metadata = new Dictionary<string, string>();
            }

            return new BlobContainerEntry
            {
                Properties = containerProperties,
                Name = name,
                Uri = NavigationHelper.AppendPathToSingleUri(baseUri, name),
                Metadata = metadata,
            };
        }

        /// <summary>
        /// Parses the response XML for a container listing operation.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="BlobContainerEntry"/> objects.</returns>
        internal static async Task<ListContainersResponse> ParseAsync(Stream stream, CancellationToken token)
        {
            using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(stream))
            {
                token.ThrowIfCancellationRequested();

                List<BlobContainerEntry> entries = new List<BlobContainerEntry>();

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

                                    case Constants.ContainersElement:
                                        await reader.ReadStartElementAsync().ConfigureAwait(false);
                                        while (await reader.IsStartElementAsync(Constants.ContainerElement).ConfigureAwait(false))
                                        {
                                            entries.Add(await ParseContainerEntryAsync(reader, baseUri, token).ConfigureAwait(false));
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

                return new ListContainersResponse { Containers = entries, NextMarker = nextMarker };
            }
        }
    }
}
