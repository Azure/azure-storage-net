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
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Provides methods for parsing the response from an operation to get a range for a file.
    /// </summary>
#if WINDOWS_DESKTOP
    public
#else
    internal
#endif
        sealed class ListRangesResponse : ResponseParsingBase<FileRange>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ListRangesResponse"/> class.
        /// </summary>
        /// <param name="stream">The stream of ranges to be parsed.</param>
        public ListRangesResponse(Stream stream)
            : base(stream)
        {
        }

        /// <summary>
        /// Gets an enumerable collection of <see cref="FileRange"/> objects from the response.
        /// </summary>
        /// <value>An enumerable collection of <see cref="FileRange"/> objects.</value>
        public IEnumerable<FileRange> Ranges
        {
            get
            {
                return this.ObjectsToParse;
            }
        }

        /// <summary>
        /// Reads a range.
        /// </summary>
        /// <returns>Range entry</returns>
        private FileRange ParseRange()
        {
            long start = 0L;
            long end = 0L;

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
                        case Constants.StartElement:
                            start = reader.ReadElementContentAsLong();
                            break;

                        case Constants.EndElement:
                            end = reader.ReadElementContentAsLong();
                            break;

                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            this.reader.ReadEndElement();

            return new FileRange(start, end);
        }

        /// <summary>
        /// Parses the XML response for an operation to get a range for a file.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="FileRange"/> objects.</returns>
        protected override IEnumerable<FileRange> ParseXml()
        {
            if (this.reader.ReadToFollowing(Constants.FileRangeListElement))
            {
                if (this.reader.IsEmptyElement)
                {
                    this.reader.Skip();
                }
                else
                {
                    this.reader.ReadStartElement();
                    while (this.reader.IsStartElement(Constants.FileRangeElement))
                    {
                        yield return this.ParseRange();
                    }

                    this.allObjectsParsed = true;
                    this.reader.ReadEndElement();
                }
            }
        }
    }
}
