// -----------------------------------------------------------------------------------------
// <copyright file="CloudQueueMessage.Common.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Queue
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Represents a message in the Microsoft Azure Queue service.
    /// </summary>
    public sealed partial class CloudQueueMessage
    {
        /// <summary>
        /// The maximum message size in bytes.
        /// </summary>
        private const long MaximumMessageSize = 64 * Constants.KB;

        /// <summary>
        /// Gets the maximum message size in bytes.
        /// </summary>
        /// <value>The maximum message size in bytes.</value>
        public static long MaxMessageSize
        {
            get
            {
                return MaximumMessageSize;
            }
        }

        /// <summary>
        /// The maximum amount of time a message is kept in the queue.
        /// </summary>
        private static readonly TimeSpan MaximumTimeToLive = TimeSpan.FromDays(7);

        /// <summary>
        /// Gets the maximum amount of time a message is kept in the queue.
        /// </summary>
        /// <value>A <see cref="TimeSpan"/> specifying the maximum amount of time a message is kept in the queue.</value>
        public static TimeSpan MaxTimeToLive
        {
            get
            {
                return MaximumTimeToLive;
            }
        }

        /// <summary>
        /// The maximum number of messages that can be peeked at a time.
        /// </summary>
        private const int MaximumNumberOfMessagesToPeek = 32;

        /// <summary>
        /// Gets the maximum number of messages that can be peeked at a time.
        /// </summary>
        /// <value>The maximum number of messages that can be peeked at a time.</value>
        public static int MaxNumberOfMessagesToPeek
        {
            get
            {
                return MaximumNumberOfMessagesToPeek;
            }
        }

        /// <summary>
        /// Custom UTF8Encoder to throw exception in case of invalid bytes.
        /// </summary>
        private static UTF8Encoding utf8Encoder = new UTF8Encoding(false, true);

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudQueueMessage"/> class with the given byte array.
        /// </summary>
        internal CloudQueueMessage()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudQueueMessage"/> class with the given string.
        /// </summary>
        /// <param name="content">The content of the message as a string of text.</param>
        public CloudQueueMessage(string content)
        {
            this.SetMessageContent(content);

            // At this point, without knowing whether or not the message will be Base64 encoded, we can't fully validate the message size.
            // So we leave it to CloudQueue so that we have a central place for this logic.
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudQueueMessage"/> class with the given message ID and pop receipt.
        /// </summary>
        /// <param name="messageId">A string specifying the message ID.</param>
        /// <param name="popReceipt">A string containing the pop receipt token.</param>
        public CloudQueueMessage(string messageId, string popReceipt)
        {
            this.Id = messageId;
            this.PopReceipt = popReceipt;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudQueueMessage"/> class with the given Base64 encoded string.
        /// This method is only used internally.
        /// </summary>
        /// <param name="content">The text string.</param>
        /// <param name="isBase64Encoded">Whether the string is Base64 encoded.</param>
        internal CloudQueueMessage(string content, bool isBase64Encoded)
        {
            if (content == null)
            {
                content = string.Empty;
            }

            this.RawString = content;
            this.MessageType = isBase64Encoded ? QueueMessageType.Base64Encoded : QueueMessageType.RawString;
        }

        /// <summary>
        /// Gets the content of the message as a byte array.
        /// </summary>
        /// <value>The content of the message as a byte array.</value>
        [SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays", Justification = "Reviewed.")]
        public byte[] AsBytes
        {
            get
            {
                if (this.MessageType == QueueMessageType.RawString)
                {
                    return Encoding.UTF8.GetBytes(this.RawString);
                }
                else if (this.MessageType == QueueMessageType.Base64Encoded)
                {
                    return Convert.FromBase64String(this.RawString);
                }
                else
                {
                    return this.RawBytes;
                }
            }
        }

        /// <summary>
        /// Gets the message ID.
        /// </summary>
        /// <value>A string containing the message ID.</value>
        public string Id { get; internal set; }

        /// <summary>
        /// Gets the message's pop receipt.
        /// </summary>
        /// <value>A string containing the pop receipt value.</value>
        public string PopReceipt { get; internal set; }

        /// <summary>
        /// Gets the time that the message was added to the queue.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> indicating the time that the message was added to the queue.</value>
        public DateTimeOffset? InsertionTime { get; internal set; }

        /// <summary>
        /// Gets the time that the message expires.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> indicating the time that the message expires.</value>
        public DateTimeOffset? ExpirationTime { get; internal set; }

        /// <summary>
        /// Gets the time that the message will next be visible.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> indicating the time that the message will next be visible.</value>
        public DateTimeOffset? NextVisibleTime { get; internal set; }

        /// <summary>
        /// Gets the content of the message, as a string.
        /// </summary>
        /// <value>A string containing the message content.</value>
        public string AsString
        {
            get
            {
                if (this.MessageType == QueueMessageType.RawString)
                {
                    return this.RawString;
                }
                else if (this.MessageType == QueueMessageType.Base64Encoded)
                {
                    byte[] messageData = Convert.FromBase64String(this.RawString);
                    return utf8Encoder.GetString(messageData, 0, messageData.Length);
                }
                else
                {
                    return utf8Encoder.GetString(this.RawBytes, 0, this.RawBytes.Length);
                }
            }
        }

        /// <summary>
        /// Gets the number of times this message has been dequeued.
        /// </summary>
        /// <value>The number of times this message has been dequeued.</value>
        public int DequeueCount { get; internal set; }

        /// <summary>
        /// Gets message type that indicates if the RawString is the original message string or Base64 encoding of the original binary data.
        /// </summary>
        internal QueueMessageType MessageType { get; private set; }

        /// <summary>
        /// Gets or sets the original message string or Base64 encoding of the original binary data.
        /// </summary>
        /// <value>The original message string.</value>
        internal string RawString { get; set; }

        /// <summary>
        /// Gets or sets the original binary data.
        /// </summary>
        /// <value>The original binary data.</value>
        internal byte[] RawBytes { get; set; }

        /// <summary>
        /// Gets the content of the message for transfer (internal use only).
        /// </summary>
        /// <param name="shouldEncodeMessage">Indicates if the message should be encoded.</param>
        /// <param name="options">A <see cref="QueueRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>The message content as a string.</returns>
        internal string GetMessageContentForTransfer(bool shouldEncodeMessage, QueueRequestOptions options = null)
        {
            if (!shouldEncodeMessage && this.MessageType != QueueMessageType.RawString)
            {
                throw new ArgumentException(SR.BinaryMessageShouldUseBase64Encoding);
            }

            string outgoingMessageString = null;

#if !(WINDOWS_RT || NETCORE)
            if (options != null)
            {
                options.AssertPolicyIfRequired();

                if (options.EncryptionPolicy != null)
                {
                    // Create an encrypted message that will hold the message contents along with encryption related metadata and return it.
                    // The encrypted message is already Base 64 encoded. So no need to process further in this method.
                    string encryptedMessageString = options.EncryptionPolicy.EncryptMessage(this.AsBytes);

                    // the size of Base64 encoded string is the number of bytes this message will take up on server.
                    if (encryptedMessageString.Length > CloudQueueMessage.MaxMessageSize)
                    {
                        throw new ArgumentException(string.Format(
                            CultureInfo.InvariantCulture,
                            SR.EncryptedMessageTooLarge,
                            CloudQueueMessage.MaxMessageSize));
                    }

                    return encryptedMessageString;
                }
            }
#endif

            if (this.MessageType != QueueMessageType.Base64Encoded)
            {
                if (shouldEncodeMessage)
                {
                    outgoingMessageString = Convert.ToBase64String(this.AsBytes);

                    // the size of Base64 encoded string is the number of bytes this message will take up on server.
                    if (outgoingMessageString.Length > CloudQueueMessage.MaxMessageSize)
                    {
                        throw new ArgumentException(string.Format(
                            CultureInfo.InvariantCulture,
                            SR.MessageTooLarge,
                            CloudQueueMessage.MaxMessageSize));
                    }
                }
                else
                {
                    outgoingMessageString = this.RawString;

                    // we need to calculate the size of its UTF8 byte array, as that will be the storage usage on server.
                    if (Encoding.UTF8.GetBytes(outgoingMessageString).Length > CloudQueueMessage.MaxMessageSize)
                    {
                        throw new ArgumentException(string.Format(
                            CultureInfo.InvariantCulture,
                            SR.MessageTooLarge,
                            CloudQueueMessage.MaxMessageSize));
                    }
                }
            }
            else
            {
                // at this point, this.EncodeMessage must be true
                outgoingMessageString = this.RawString;

                // the size of Base64 encoded string is the number of bytes this message will take up on server.
                if (outgoingMessageString.Length > CloudQueueMessage.MaxMessageSize)
                {
                    throw new ArgumentException(string.Format(
                        CultureInfo.InvariantCulture,
                        SR.MessageTooLarge,
                        CloudQueueMessage.MaxMessageSize));
                }
            }

            return outgoingMessageString;
        }

        /// <summary>
        /// Sets the content of this message.
        /// </summary>
        /// <param name="content">A string containing the new message content.</param>
        public void SetMessageContent(string content)
        {
            if (content == null)
            {
                // Protocol will return null for empty content
                content = string.Empty;
            }

            this.RawString = content;
            this.MessageType = QueueMessageType.RawString;
        }
    }
}
