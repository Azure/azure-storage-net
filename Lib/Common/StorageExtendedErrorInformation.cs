// -----------------------------------------------------------------------------------------
// <copyright file="StorageExtendedErrorInformation.cs" company="Microsoft">
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


namespace Microsoft.Azure.Storage
{
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
#if WINDOWS_RT || NETCORE
    using System.Net.Http;
#endif

#if WINDOWS_RT
    using Windows.Storage.Streams;
#endif

    /// <summary>
    /// Represents extended error information returned by the Microsoft Azure storage services.
    /// </summary>
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
    [Serializable]
#endif
    public sealed class StorageExtendedErrorInformation
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StorageExtendedErrorInformation"/> class.
        /// </summary>
        public StorageExtendedErrorInformation()
        {
        }

        /// <summary>
        /// Gets the storage service error code.
        /// </summary>
        /// <value>A string containing the storage service error code.</value>
        public string ErrorCode { get; internal set; }

        /// <summary>
        /// Gets the storage service error message.
        /// </summary>
        /// <value>A string containing the storage service error message.</value>
        public string ErrorMessage { get; internal set; }

        /// <summary>
        /// Gets additional error details from XML-formatted input stream.
        /// </summary>
        /// <value>An <see cref="IDictionary{TKey,TValue}"/> containing the additional error details.</value>
        public IDictionary<string, string> AdditionalDetails { get; internal set; }

#if WINDOWS_RT
        public static Task<StorageExtendedErrorInformation> ReadFromStreamAsync(IInputStream inputStream)
        {
            return ReadFromStreamAsync(inputStream.AsStreamForRead());
        }
#endif

        /// <summary>
        /// Gets the error details from an XML-formatted error stream.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <returns>The error details.</returns>
        public static async Task<StorageExtendedErrorInformation> ReadFromStreamAsync(Stream inputStream)
        {
            CommonUtility.AssertNotNull("inputStream", inputStream);

            if (inputStream.CanSeek && inputStream.Length < 1)
            {
                return null;
            }

            StorageExtendedErrorInformation extendedErrorInfo = new StorageExtendedErrorInformation();
            try
            {
                using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(inputStream))
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                    await extendedErrorInfo.ReadXmlAsync(reader, CancellationToken.None).ConfigureAwait(false);
                }

                return extendedErrorInfo;
            }
            catch (XmlException)
            {
                // If there is a parsing error we cannot return extended error information
                return null;
            }
        }

        /// <summary>
        /// Gets the error details from an XML-formatted error stream.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <returns>The error details.</returns>
        public static async Task<StorageExtendedErrorInformation> ReadFromStreamAsync(Stream inputStream, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("inputStream", inputStream);

            if (inputStream.CanSeek && inputStream.Length < 1)
            {
                return null;
            }

            StorageExtendedErrorInformation extendedErrorInfo = new StorageExtendedErrorInformation();
            try
            {
                using (XmlReader reader = XmlReader.Create(inputStream, new XmlReaderSettings { Async = true }))
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();

                    await extendedErrorInfo.ReadXmlAsync(reader, cancellationToken).ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                }

                return extendedErrorInfo;
            }
            catch (XmlException)
            {
                // If there is a parsing error we cannot return extended error information
                return null;
            }
        }

#region IXmlSerializable

        /// <summary>
        /// Generates a serializable <see cref="StorageExtendedErrorInformation"/> object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader"/> stream from which the <see cref="StorageExtendedErrorInformation"/> object is deserialized.</param>
#if WINDOWS_RT
        internal
#else
        public
#endif
        /// <summary>
        /// Generates a serializable <see cref="StorageExtendedErrorInformation"/> object from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader"/> stream from which the <see cref="StorageExtendedErrorInformation"/> object is deserialized.</param>
        async Task ReadXmlAsync(XmlReader reader, CancellationToken cancellationToken)
        {
            CommonUtility.AssertNotNull("reader", reader);

            this.AdditionalDetails = new Dictionary<string, string>();

            await reader.ReadStartElementAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            while (await reader.IsStartElementAsync().ConfigureAwait(false))
            {
                if (reader.IsEmptyElement)
                {
                    await reader.SkipAsync().ConfigureAwait(false);
                    cancellationToken.ThrowIfCancellationRequested();
                }
                else
                {
                    if ((string.Compare(reader.LocalName, Constants.ErrorCode, StringComparison.OrdinalIgnoreCase) == 0) || (string.Compare(reader.LocalName, Constants.ErrorCodePreview, StringComparison.Ordinal) == 0))
                    {
                        this.ErrorCode = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    else if ((string.Compare(reader.LocalName, Constants.ErrorMessage, StringComparison.OrdinalIgnoreCase) == 0) || (string.Compare(reader.LocalName, Constants.ErrorMessagePreview, StringComparison.Ordinal) == 0))
                    {
                        this.ErrorMessage = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    else if (string.Compare(reader.LocalName, Constants.ErrorException, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        await reader.ReadStartElementAsync(null, null).ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                        while (await reader.IsStartElementAsync().ConfigureAwait(false))
                        {
                            switch (reader.LocalName)
                            {
                                case Constants.ErrorExceptionMessage:
                                    this.AdditionalDetails.Add(
                                    Constants.ErrorExceptionMessage,
                                    await reader.ReadElementContentAsStringAsync(Constants.ErrorExceptionMessage, string.Empty).ConfigureAwait(false));
                                    cancellationToken.ThrowIfCancellationRequested();
                                    break;

                                case Constants.ErrorExceptionStackTrace:
                                    this.AdditionalDetails.Add(
                                    Constants.ErrorExceptionStackTrace,
                                    await reader.ReadElementContentAsStringAsync(Constants.ErrorExceptionStackTrace, string.Empty).ConfigureAwait(false));
                                    cancellationToken.ThrowIfCancellationRequested();
                                    break;

                                default:
                                    await reader.SkipAsync().ConfigureAwait(false);
                                    cancellationToken.ThrowIfCancellationRequested();
                                    break;
                            }
                        }

                        await reader.ReadEndElementAsync().ConfigureAwait(false);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    else
                    {
                        this.AdditionalDetails.Add(
                        reader.LocalName,
                        await reader.ReadInnerXmlAsync().ConfigureAwait(false));
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                }
            }

            await reader.ReadEndElementAsync().ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
        }

        /// <summary>
        /// Converts a serializable <see cref="StorageExtendedErrorInformation"/> object into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter"/> stream to which the <see cref="StorageExtendedErrorInformation"/> object is serialized.</param>
#if WINDOWS_RT
        internal
#else
        public
#endif
 void WriteXml(XmlWriter writer)
        {
            CommonUtility.AssertNotNull("writer", writer);

            writer.WriteStartElement(Constants.ErrorRootElement);
#pragma warning disable 618
            writer.WriteElementString(Constants.ErrorCode, this.ErrorCode);
#pragma warning restore 618
            writer.WriteElementString(Constants.ErrorMessage, this.ErrorMessage);

            foreach (string key in this.AdditionalDetails.Keys)
            {
                writer.WriteElementString(key, this.AdditionalDetails[key]);
            }

            // End StorageExtendedErrorInformation
            writer.WriteEndElement();
        }

#endregion
    }
}