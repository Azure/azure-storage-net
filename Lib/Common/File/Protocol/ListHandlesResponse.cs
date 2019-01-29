//----------------------------------------------------------------------- 
// <copyright file="ListHandlesResponse.cs" company="Microsoft"> 
//    Copyright 2018 Microsoft Corporation 
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
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;

    /// <summary> 
    /// Provides methods for parsing the response from an operation to get a range for a file. 
    /// </summary> 
#if WINDOWS_DESKTOP
    public 
#else
    internal
#endif
        sealed class ListHandlesResponse : ResponseParsingBase<FileHandle>
    {
        private string nextMarker;
        private bool nextMarkerConsumable;
        private int? maxResults;
        private bool maxResultsConsumable;

        /// <summary> 
        /// Initializes a new instance of the <see cref="ListHandlesResponse"/> class. 
        /// </summary> 
        /// <param name="stream">The stream of handles to be parsed.</param> 
        public ListHandlesResponse(Stream stream)
            : base(stream)
        {
        }

        /// <summary> 
        /// Gets an enumerable collection of <see cref="FileHandle"/> objects from the response. 
        /// </summary> 
        /// <value>An enumerable collection of <see cref="FileHandle"/> objects.</value> 
        public IEnumerable<FileHandle> Handles
        {
            get
            {
                return this.ObjectsToParse;
            }
        }

        /// <summary> 
        /// Gets the NextMarker value from the XML response, if the listing was not complete. 
        /// </summary> 
        /// <value>A string containing the NextMarker value, should one exist.</value> 
        public string NextMarker
        {
            get
            {
                this.Variable(ref this.nextMarkerConsumable);

                return this.nextMarker;
            }
        }

        /// <summary> 
        /// Gets the MaxResults value from the XML response, if one was specified. 
        /// </summary> 
        /// <value>A nullable int containing the MaxResults value, should one exist.</value> 
        public int? MaxResults
        {
            get
            {
                this.Variable(ref this.maxResultsConsumable);
                return this.maxResults;
            }
        }

        /// <summary> 
        /// Reads a handle. 
        /// </summary> 
        /// <returns>Range entry</returns> 
        private FileHandle ParseHandle()
        {
            var handle = new FileHandle();

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
                        case Constants.HandleIdElement:
                            ulong handleId;
                            if (ulong.TryParse(reader.ReadElementContentAsString(), out handleId))
                            {
                                handle.HandleId = handleId;
                            }

                            break;
                        case Constants.PathElement:
                            handle.Path = reader.ReadElementContentAsString();
                            break;
                        case Constants.ClientIpElement:
                            IPAddress clientIp;
                            if (IPAddress.TryParse(reader.ReadElementContentAsString(), out clientIp))
                            {
                                handle.ClientIp = clientIp;
                            }

                            break;
                        case Constants.OpenTimeElement:
                            DateTimeOffset openTime;
                            if (DateTimeOffset.TryParse(reader.ReadElementContentAsString(), out openTime))
                            {
                                handle.OpenTime = openTime;
                            }

                            break;
                        case Constants.LastReconnectTimeElement:
                            DateTimeOffset lastReconnectTime;
                            if (DateTimeOffset.TryParse(reader.ReadElementContentAsString(), out lastReconnectTime))
                            {
                                handle.LastReconnectTime = lastReconnectTime;
                            }

                            break;
                        case Constants.FileIdElement:
                            ulong fileId;
                            if (ulong.TryParse(reader.ReadElementContentAsString(), out fileId))
                            {
                                handle.FileId = fileId;
                            }

                            break;
                        case Constants.ParentIdElement:
                            ulong parentFileId;
                            if (ulong.TryParse(reader.ReadElementContentAsString(), out parentFileId))
                            {
                                handle.ParentId = parentFileId;
                            }

                            break;
                        case Constants.SessionIdElement:
                            ulong sessionId;
                            if (ulong.TryParse(reader.ReadElementContentAsString(), out sessionId))
                            {
                                handle.SessionId = sessionId;
                            }

                            break;
                        default:
                            reader.Skip();
                            break;
                    }
                }
            }

            this.reader.ReadEndElement();

            return handle;
        }

        /// <summary> 
        /// Parses the XML response for an operation to get a handle for a file. 
        /// </summary> 
        /// <returns>An enumerable collection of <see cref="FileHandle"/> objects.</returns> 
        protected override IEnumerable<FileHandle> ParseXml()
        {
            if (this.reader.ReadToFollowing(Constants.EnumerationResultsElement))
            {
                if (this.reader.IsEmptyElement)
                {
                    this.reader.Skip();
                }
                else
                {
                    this.reader.ReadStartElement();

                    while (this.reader.IsStartElement())
                    {
                        while (this.reader.IsEmptyElement)
                        {
                            this.reader.Skip();
                        }

                        switch (this.reader.Name)
                        {
                            case Constants.EntriesElement:
                                this.reader.ReadStartElement();

                                while (this.reader.IsStartElement())
                                {
                                    switch (this.reader.Name)
                                    {
                                        case Constants.HandleElement:
                                            yield return this.ParseHandle();
                                            break;
                                    }
                                }

                                this.reader.ReadEndElement();
                                break;
                            case Constants.NextMarkerElement:
                                this.nextMarker = reader.ReadElementContentAsString();
                                this.nextMarkerConsumable = true;

                                yield return null;
                                break;
                            case Constants.MaxResults:
                                this.maxResults = reader.ReadElementContentAsInt();
                                this.maxResultsConsumable = true;

                                yield return null;
                                break;
                            default:
                                reader.Skip();
                                break;
                        }
                    }

                    this.allObjectsParsed = true;
                }
            }
        }
    }
}