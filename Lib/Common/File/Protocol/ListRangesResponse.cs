//-----------------------------------------------------------------------
// <copyright file="ListRangesResponse.cs" company="Microsoft">
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
    using Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Provides methods for parsing the response from an operation to get a range for a file.
    /// </summary>
#if WINDOWS_DESKTOP
    public
#else
    internal
#endif
        static class ListRangesResponse
    {
        /// <summary>
        /// Reads a range.
        /// </summary>
        /// <returns>Range entry</returns>
        private static async Task<FileRange> ParseRangeAsync(XmlReader reader, CancellationToken token)
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
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
            }

            await reader.ReadEndElementAsync().ConfigureAwait(false);

            return new FileRange(start, end);
        }

        /// <summary>
        /// Parses the XML response for an operation to get a range for a file.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="FileRange"/> objects.</returns>
        internal static async Task<IEnumerable<FileRange>> ParseAsync(Stream stream, CancellationToken token)
        {
            using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(stream))
            {
                token.ThrowIfCancellationRequested();

                List<FileRange> ranges = new List<FileRange>();

                if (await reader.ReadToFollowingAsync(Constants.FileRangeListElement).ConfigureAwait(false))
                {
                    if (reader.IsEmptyElement)
                    {
                        await reader.SkipAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        await reader.ReadStartElementAsync().ConfigureAwait(false);
                        while (await reader.IsStartElementAsync(Constants.FileRangeElement).ConfigureAwait(false))
                        {
                            ranges.Add(await ParseRangeAsync(reader, token).ConfigureAwait(false));
                        }

                        await reader.ReadEndElementAsync().ConfigureAwait(false);
                    }
                }

                return ranges;
            }
        }
    }
}
