//-----------------------------------------------------------------------
// <copyright file="GetBlockListResponse.cs" company="Microsoft">
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
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Provides methods for parsing the response from an operation to return a block list.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
    static class GetBlockListResponse
    {
        /// <summary>
        /// Asynchronously parses the XML response returned by an operation to retrieve a list of blocks.
        /// </summary>
        /// <param name="stream">The stream containing the XML response.</param>
        /// <param name="token">The cancellation token.</param>
        /// <returns>The list of <see cref="ListBlockItem"/> objects.</returns>
        internal static async Task<IEnumerable<ListBlockItem>> ParseAsync(Stream stream, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            List<ListBlockItem> blocks = new List<ListBlockItem>();

            using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(stream))
            {
                token.ThrowIfCancellationRequested();
                if (await reader.ReadToFollowingAsync(Constants.BlockListElement).ConfigureAwait(false))
                {
                    if (reader.IsEmptyElement)
                    {
                        await reader.SkipAsync().ConfigureAwait(false);
                    }
                    else
                    {
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
                                    case Constants.CommittedBlocksElement:
                                        await reader.ReadStartElementAsync().ConfigureAwait(false);
                                        while (await reader.IsStartElementAsync(Constants.BlockElement).ConfigureAwait(false))
                                        {
                                            token.ThrowIfCancellationRequested();
                                            blocks.Add(await ParseBlockItemAsync(true, reader, token).ConfigureAwait(false));
                                        }

                                        await reader.ReadEndElementAsync().ConfigureAwait(false);
                                        break;

                                    case Constants.UncommittedBlocksElement:
                                        await reader.ReadStartElementAsync().ConfigureAwait(false);
                                        while (await reader.IsStartElementAsync(Constants.BlockElement).ConfigureAwait(false))
                                        {
                                            token.ThrowIfCancellationRequested();
                                            blocks.Add(await ParseBlockItemAsync(false, reader, token).ConfigureAwait(false));
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
                return blocks;
            }
        }

        /// <summary>
        /// Reads a block item for block listing.
        /// </summary>
        /// <param name="committed">Whether we are currently listing committed blocks or not</param>
        /// <param name="reader"></param>
        /// <param name="token"></param>
        /// <returns>Block listing entry</returns>
        private static async Task<ListBlockItem> ParseBlockItemAsync(bool committed, XmlReader reader, CancellationToken token)
        {
            ListBlockItem block = new ListBlockItem()
            {
                Committed = committed,
            };

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
                        case Constants.SizeElement:
                            block.Length = await reader.ReadElementContentAsInt64Async().ConfigureAwait(false);
                            break;

                        case Constants.NameElement:
                            block.Name = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            break;

                        default:
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
            }

            await reader.ReadEndElementAsync().ConfigureAwait(false);

            return block;
        }
    }
}