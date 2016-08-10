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


namespace Microsoft.WindowsAzure.Storage
{
    using Microsoft.Data.OData;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table.Protocol;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
#if WINDOWS_RT   || NETCORE
    using System.Net.Http;
#endif
    using System.Xml;

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
    using Microsoft.WindowsAzure.Storage.Table.DataServices;
#elif WINDOWS_RT
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
        public static StorageExtendedErrorInformation ReadFromStream(IInputStream inputStream)
        {
            return ReadFromStream(inputStream.AsStreamForRead());
        }
#endif

        /// <summary>
        /// Gets the error details from an XML-formatted error stream.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <returns>The error details.</returns>
        public static StorageExtendedErrorInformation ReadFromStream(Stream inputStream)
        {
            CommonUtility.AssertNotNull("inputStream", inputStream);

            if (inputStream.CanSeek && inputStream.Length < 1)
            {
                return null;
            }
            
            StorageExtendedErrorInformation extendedErrorInfo = new StorageExtendedErrorInformation();
            try
            {               
                using (XmlReader reader = XmlReader.Create(inputStream))
                {
                    reader.Read();
                    extendedErrorInfo.ReadXml(reader);
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
        /// Gets the error details from the stream using OData library.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <param name="response">The web response.</param>
        /// <param name="contentType">The response Content-Type.</param>
        /// <returns>The error details.</returns>
#if WINDOWS_RT || NETCORE
        public static StorageExtendedErrorInformation ReadFromStreamUsingODataLib(Stream inputStream, HttpResponseMessage response, string contentType)
#else
        public static StorageExtendedErrorInformation ReadFromStreamUsingODataLib(Stream inputStream, HttpWebResponse response, string contentType)
#endif
        {
            CommonUtility.AssertNotNull("inputStream", inputStream);
            CommonUtility.AssertNotNull("response", response);

            if (inputStream.CanSeek && inputStream.Length <= 0)
            {
                return null;
            }

            HttpResponseAdapterMessage responseMessage = new HttpResponseAdapterMessage(response, new NonCloseableStream(inputStream), contentType);
            return ReadAndParseExtendedError(responseMessage);
        }

        /// <summary>
        /// Gets the error details from the stream using OData library.
        /// </summary>
        /// <param name="inputStream">The input stream.</param>
        /// <param name="responseHeaders">The web response headers.</param>
        /// <param name="contentType">The response Content-Type.</param>
        /// <returns>The error details.</returns>
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
        public static StorageExtendedErrorInformation ReadDataServiceResponseFromStream(Stream inputStream, IDictionary<string, string> responseHeaders, string contentType)
        {
            CommonUtility.AssertNotNull("inputStream", inputStream);

            if (inputStream.CanSeek && inputStream.Length <= 0)
            {
                return null;
            }

            DataServicesResponseAdapterMessage responseMessage = new DataServicesResponseAdapterMessage(responseHeaders, inputStream, contentType);
            return ReadAndParseExtendedError(responseMessage);
        }
#endif

        /// <summary>
        /// Parses the error details from the stream using OData library.
        /// </summary>
        /// <param name="responseMessage">The IODataResponseMessage to parse.</param>
        /// <returns>The error details.</returns>
        public static StorageExtendedErrorInformation ReadAndParseExtendedError(IODataResponseMessage responseMessage)
        {
            StorageExtendedErrorInformation storageExtendedError = null;
            using (ODataMessageReader reader = new ODataMessageReader(responseMessage))
            {
                try
                {
                    ODataError error = reader.ReadError();
                    if (error != null)
                    {
                        storageExtendedError = new StorageExtendedErrorInformation();
                        storageExtendedError.AdditionalDetails = new Dictionary<string, string>();
                        storageExtendedError.ErrorCode = error.ErrorCode;
                        storageExtendedError.ErrorMessage = error.Message;
                        if (error.InnerError != null)
                        {
                            storageExtendedError.AdditionalDetails[Constants.ErrorExceptionMessage] = error.InnerError.Message;
                            storageExtendedError.AdditionalDetails[Constants.ErrorExceptionStackTrace] = error.InnerError.StackTrace;
                        }

#if !(WINDOWS_PHONE && WINDOWS_DESKTOP)
                        if (error.InstanceAnnotations.Count > 0)
                        {
                            foreach (ODataInstanceAnnotation annotation in error.InstanceAnnotations)
                            {
                                storageExtendedError.AdditionalDetails[annotation.Name] = annotation.Value.GetAnnotation<string>();
                            }
                        }
#endif
                    }
                }
                catch (Exception)
                {
                    // Error cannot be parsed. 
                    return null;
                }
            }

            return storageExtendedError;
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
        void ReadXml(XmlReader reader)
        {
            CommonUtility.AssertNotNull("reader", reader);

            this.AdditionalDetails = new Dictionary<string, string>();

            reader.ReadStartElement();
            while (reader.IsStartElement())
            {
                if (reader.IsEmptyElement)
                {
                    reader.Skip();
                }
                else
                {
                    if ((string.Compare(reader.LocalName, Constants.ErrorCode, StringComparison.OrdinalIgnoreCase) == 0) || (string.Compare(reader.LocalName, Constants.ErrorCodePreview, StringComparison.Ordinal) == 0))
                    {
                        this.ErrorCode = reader.ReadElementContentAsString();
                    }
                    else if ((string.Compare(reader.LocalName, Constants.ErrorMessage, StringComparison.OrdinalIgnoreCase) == 0) || (string.Compare(reader.LocalName, Constants.ErrorMessagePreview, StringComparison.Ordinal) == 0))
                    {
                        this.ErrorMessage = reader.ReadElementContentAsString();
                    }
                    else if (string.Compare(reader.LocalName, Constants.ErrorException, StringComparison.OrdinalIgnoreCase) == 0)
                    {
                        reader.ReadStartElement();
                        while (reader.IsStartElement())
                        {
                            switch (reader.LocalName)
                            {
                                case Constants.ErrorExceptionMessage:
                                    this.AdditionalDetails.Add(
                                    Constants.ErrorExceptionMessage,
                                    reader.ReadElementContentAsString(Constants.ErrorExceptionMessage, string.Empty));
                                    break;

                                case Constants.ErrorExceptionStackTrace:
                                    this.AdditionalDetails.Add(
                                    Constants.ErrorExceptionStackTrace,
                                    reader.ReadElementContentAsString(Constants.ErrorExceptionStackTrace, string.Empty));
                                    break;

                                default:
                                    reader.Skip();
                                    break;
                            }
                        }

                        reader.ReadEndElement();
                    }
                    else
                    {
                        this.AdditionalDetails.Add(
                        reader.LocalName,
                        reader.ReadInnerXml());
                    }
                }
            }

            reader.ReadEndElement();
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
            writer.WriteElementString(Constants.ErrorCode, this.ErrorCode);
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