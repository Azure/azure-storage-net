namespace Microsoft.Azure.Storage.Queue.Protocol
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
    /// Provides methods for parsing the response from a queue listing operation.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
 sealed class ListQueuesResponse
    {
        /// <summary>
        /// Gets an enumerable collection of <see cref="QueueEntry"/> objects from the response.
        /// </summary>
        /// <value>An enumerable collection of <see cref="QueueEntry"/> objects.</value>
        public IEnumerable<QueueEntry> Queues { get; private set; }

/// <summary>
        /// Gets the NextMarker value from the XML response, if the listing was not complete.
        /// </summary>
        /// <value>The NextMarker value.</value>
        public string NextMarker { get; private set; }

        /// <summary>
        /// Parses a queue entry in a queue listing response.
        /// </summary>
        /// <returns>Queue listing entry</returns>
        private static async Task<QueueEntry> ParseQueueEntryAsync(XmlReader reader, Uri baseUri, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            string name = null;
            IDictionary<string, string> metadata = null;

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

            return new QueueEntry(
                name, 
                NavigationHelper.AppendPathToSingleUri(baseUri, name), 
                metadata ?? new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                );
        }

        /// <summary>
        /// Parses the response XML for a queue listing operation.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="QueueEntry"/> objects.</returns>
        internal static async Task<ListQueuesResponse> ParseAsync(Stream stream, CancellationToken token)
        {
            using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(stream))
            {
                token.ThrowIfCancellationRequested();

                List<QueueEntry> entries = new List<QueueEntry>();
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

                                    case Constants.QueuesElement:
                                        await reader.ReadStartElementAsync().ConfigureAwait(false);
                                        while (await reader.IsStartElementAsync().ConfigureAwait(false))
                                        {
                                            entries.Add(await ParseQueueEntryAsync(reader, baseUri, token).ConfigureAwait(false));
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

                return new ListQueuesResponse { Queues = entries, NextMarker = nextMarker };
            }
        }
    }
}