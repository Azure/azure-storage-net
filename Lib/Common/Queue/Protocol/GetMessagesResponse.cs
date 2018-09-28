// -----------------------------------------------------------------------------------------
// <copyright file="GetMessagesResponse.cs" company="Microsoft">
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
// -----------------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.Queue.Protocol
{
    using Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Provides methods for parsing the response from an operation to get messages from a queue.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
 static class GetMessagesResponse
    {
        /// <summary>
        /// Parses a message entry in a queue get messages response.
        /// </summary>
        /// <returns>Message entry</returns>
        private static async Task<QueueMessage> ParseMessageEntryAsync(XmlReader reader, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            QueueMessage message = null;
            string id = null;
            string popReceipt = null;
            DateTimeOffset? insertionTime = null;
            DateTimeOffset? expirationTime = null;
            DateTimeOffset? timeNextVisible = null;
            string text = null;
            int dequeueCount = 0;

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
                        case Constants.MessageIdElement:
                            id = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            break;

                        case Constants.PopReceiptElement:
                            popReceipt = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            break;

                        case Constants.InsertionTimeElement:
                            insertionTime = await reader.ReadElementContentAsDateTimeOffsetAsync().ConfigureAwait(false);
                            break;

                        case Constants.ExpirationTimeElement:
                            expirationTime = await reader.ReadElementContentAsDateTimeOffsetAsync().ConfigureAwait(false);
                            break;

                        case Constants.TimeNextVisibleElement:
                            timeNextVisible = await reader.ReadElementContentAsDateTimeOffsetAsync().ConfigureAwait(false);
                            break;

                        case Constants.MessageTextElement:
                            text = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                            break;

                        case Constants.DequeueCountElement:
                            dequeueCount = await reader.ReadElementContentAsInt32Async().ConfigureAwait(false);
                            break;

                        default:
                            await reader.SkipAsync().ConfigureAwait(false);
                            break;
                    }
                }
            }

            await reader.ReadEndElementAsync().ConfigureAwait(false);
            message =
                new QueueMessage
                {
                    Text = text,
                    Id = id,
                    PopReceipt = popReceipt,
                    DequeueCount = dequeueCount,
                };

            if (insertionTime != null)
            {
                message.InsertionTime = (DateTimeOffset)insertionTime;
            }

            if (expirationTime != null)
            {
                message.ExpirationTime = (DateTimeOffset)expirationTime;
            }

            if (timeNextVisible != null)
            {
                message.NextVisibleTime = (DateTimeOffset)timeNextVisible;
            }

            return message;
        }

        /// <summary>
        /// Parses the XML response returned by an operation to get messages from a queue.
        /// </summary>
        /// <returns>An enumerable collection of <see cref="QueueMessage"/> objects.</returns>
        internal static async Task<IEnumerable<QueueMessage>> ParseAsync(Stream stream, CancellationToken token)
        {
            using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(stream))
            {
                token.ThrowIfCancellationRequested();

                List<QueueMessage> messages = new List<QueueMessage>();

                if (await reader.ReadToFollowingAsync(Constants.MessagesElement).ConfigureAwait(false))
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
                                    case Constants.MessageElement:
                                        while (await reader.IsStartElementAsync().ConfigureAwait(false))
                                        {
                                            messages.Add(await ParseMessageEntryAsync(reader, token).ConfigureAwait(false));
                                        }

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

                return messages;
            }
        }
    }
}