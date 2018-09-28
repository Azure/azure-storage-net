//-----------------------------------------------------------------------
// <copyright file="GetPageDiffRangesResponse.cs" company="Microsoft">
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
    using Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Provides methods for parsing the response from an operation to get a range of differing pages for a page blob.
    /// </summary>
#if WINDOWS_DESKTOP || NETCORE
    public
#else
    internal
#endif
        static class GetPageDiffRangesResponse
    {
        /// <summary>
        /// Reads a page range.
        /// </summary>
        /// <returns>Page range entry</returns>
        private static async Task<PageDiffRange> ParsePageDiffRangeAsync(XmlReader reader, bool isCleared, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            long start = 0L;
            long end = 0L;
  
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
                        case Constants.StartElement:
                            start = await reader.ReadElementContentAsInt64Async().ConfigureAwait(false);
                            break;

                        case Constants.EndElement:
                            end = await reader.ReadElementContentAsInt64Async().ConfigureAwait(false);
                            break;

                        default:
                            await reader.SkipAsync();
                            break;
                    }
                }
            }

            await reader.ReadEndElementAsync().ConfigureAwait(false);

            return new PageDiffRange(start, end, isCleared);
        }

        /// <summary>
        /// Parses the XML response for an operation to get a range of pages for a page blob.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="PageRange"/> objects.</returns>
        internal static async Task<IEnumerable<PageDiffRange>> ParseAsync(Stream stream, CancellationToken token)
        {
            using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(stream))
            {
                token.ThrowIfCancellationRequested();
                
                List<PageDiffRange> ranges = new List<PageDiffRange>();

                if (await reader.ReadToFollowingAsync(Constants.PageListElement).ConfigureAwait(false))
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

                            if (await reader.IsStartElementAsync(Constants.ClearRangeElement).ConfigureAwait(false))
                            {
                                ranges.Add(await ParsePageDiffRangeAsync(reader, true /* isClear */, token));
                            }
                            else if (await reader.IsStartElementAsync(Constants.PageRangeElement).ConfigureAwait(false))
                            {
                                ranges.Add(await ParsePageDiffRangeAsync(reader, false /* isClear */, token).ConfigureAwait(false));
                            }
                        }
                        
                        await reader.ReadEndElementAsync().ConfigureAwait(false);
                    }
                }

                return ranges;
            }
        }
    }
}
