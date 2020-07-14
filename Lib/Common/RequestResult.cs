// -----------------------------------------------------------------------------------------
// <copyright file="RequestResult.cs" company="Microsoft">
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
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    /// <summary>
    /// Represents the result of a physical request.
    /// </summary>
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
    [Serializable]
#endif
    public class RequestResult
    {
        private volatile Exception exception = null;

        /// <summary>
        /// Gets or sets the HTTP status code for the request.
        /// </summary>
        /// <value>The HTTP status code for the request.</value>
        public int HttpStatusCode { get; set; }

        /// <summary>
        /// Gets the HTTP status message for the request.
        /// </summary>
        /// <value>The HTTP status message for the request.</value>
        public string HttpStatusMessage { get; internal set; }

        /// <summary>
        /// Gets the service request ID for this request.
        /// </summary>
        /// <value>The service request ID for this request.</value>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "ID", Justification = "Back compatibility.")]
        public string ServiceRequestID { get; internal set; }

        /// <summary>
        /// Gets the content-MD5 value for the request. 
        /// </summary>
        /// <value>The content-MD5 value for the request.</value>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Md", Justification = "Back compatibility.")]
        public string ContentMd5 { get; internal set; }

        /// <summary>
        /// Gets the content-CRC64 value for the request. 
        /// </summary>
        /// <value>The content-CRC645 value for the request.</value>
        [SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "Crc", Justification = "Back compatibility.")]
        public string ContentCrc64 { get; internal set; }

        /// <summary>
        /// Gets the ETag value of the request.
        /// </summary>
        /// <value>The ETag value of the request.</value>
        [SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Etag", Justification = "Reviewed: Etag can be used for identifier names.")]
        public string Etag { get; internal set; }

#if !(WINDOWS_RT || NETCORE)
        /// <summary>
        /// The number of bytes read from the response body for the given request
        /// </summary>
        public long IngressBytes { get; set; }

        /// <summary>        
        /// The number of bytes written to the request body for a given request
        /// </summary>
        public long EgressBytes { get; set; }
#endif
        /// <summary>
        /// Gets the request date.
        /// </summary>
        /// <value>The request date.</value>
        public string RequestDate { get; internal set; }

        /// <summary>
        /// Gets the location to which the request was sent.
        /// </summary>
        /// <value>A <see cref="StorageLocation"/> enumeration value.</value>
        public StorageLocation TargetLocation { get; internal set; }

        /// <summary>
        /// Gets the extended error information.
        /// </summary>
        /// <value>A <see cref="StorageExtendedErrorInformation"/> object.</value>
        public StorageExtendedErrorInformation ExtendedErrorInformation { get; internal set; }

        /// <summary>
        /// Gets the storage service error code.
        /// </summary>
        /// <value>A string containing the storage service error code.</value>
        public string ErrorCode { get; internal set; }

        /// <summary>
        /// Gets whether or not the data for a write operation is encrypted server-side.
        /// </summary>
        public bool IsRequestServerEncrypted { get; internal set; }

        /// <summary>
        /// Represents whether or not the data for a read operation is encrypted on the server-side.
        /// </summary>
        public bool IsServiceEncrypted { get; internal set; }

        /// <summary>
        /// Represents the hash for the key used to server-side encrypt with client-provided keys.
        /// </summary>
        public string EncryptionKeySHA256 { get; internal set; }

        /// <summary>
        /// Represents encryption scope.
        /// </summary>
        public string EncryptionScope { get; internal set; }

        /// <summary>
        /// Gets or sets the exception.
        /// </summary>
        /// <value>An <see cref="System.Exception"/> object.</value>
        public Exception Exception
        {
            get
            {
                return this.exception;
            }

            set
            {
#if WINDOWS_RT || NETCORE
                this.ExceptionInfo = (value != null) ? new ExceptionInfo(value) : null;            
#endif
                this.exception = value;
            }
        }

#if WINDOWS_RT || NETCORE
        public DateTimeOffset StartTime { get; internal set; }

        public DateTimeOffset EndTime { get; internal set; }

        public ExceptionInfo ExceptionInfo { get; internal set; }
#else
        /// <summary>
        /// Gets the start time of the operation.
        /// </summary>
        /// <value>A <see cref="DateTime"/> value indicating the start time of the operation.</value>
        public DateTime StartTime { get; internal set; }

        /// <summary>
        /// Gets the end time of the operation.
        /// </summary>
        /// <value>A <see cref="DateTime"/> value indicating the end time of the operation.</value>
        public DateTime EndTime { get; internal set; }
#endif
        /// <summary>
        /// Translates the specified message into a <see cref="RequestResult"/> object.
        /// </summary>
        /// <param name="message">The message to translate.</param>
        /// <returns>The translated <see cref="RequestResult"/>.</returns>
#if WINDOWS_DESKTOP
        [Obsolete("This should be available only in Microsoft.Azure.Storage.WinMD and not in Microsoft.Azure.Storage.dll. Please use ReadXML to deserialize RequestResult when Microsoft.Azure.Storage.dll is used.")]
#endif
        public static RequestResult TranslateFromExceptionMessage(string message)
        {
            RequestResult res = new RequestResult();

            using (XmlReader reader = XmlReader.Create(
                new StringReader(message),
                new XmlReaderSettings
                {
                    IgnoreWhitespace = true,
                    Async = true
                }
                ))
            {
                res.ReadXmlAsync(reader).GetAwaiter().GetResult();
            }

            return res;
        }

        internal string WriteAsXml()
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            StringBuilder sb = new StringBuilder();
            try
            {
                using (XmlWriter writer = XmlWriter.Create(sb, settings))
                {
                    this.WriteXml(writer);
                }

                return sb.ToString();
            }
            catch (XmlException)
            {
                return null;
            }
        }

        #region XML Serializable

        /// <summary>
        /// Generates a serializable RequestResult from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader"/> stream from which the RequestResult is deserialized.</param>
#if WINDOWS_RT
        internal
#else
        public
#endif
        async Task ReadXmlAsync(XmlReader reader)
        {
            CommonUtility.AssertNotNull("reader", reader);

            await reader.ReadAsync().ConfigureAwait(false);

            if (reader.NodeType == XmlNodeType.Comment)
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }

            await reader.ReadStartElementAsync().ConfigureAwait(false);

            this.HttpStatusCode = int.Parse(await CommonUtility.ReadElementAsStringAsync("HTTPStatusCode", reader).ConfigureAwait(false), CultureInfo.InvariantCulture);
            this.HttpStatusMessage = await CommonUtility.ReadElementAsStringAsync("HttpStatusMessage", reader).ConfigureAwait(false);

            StorageLocation targetLocation;
            if (Enum.TryParse<StorageLocation>(await CommonUtility.ReadElementAsStringAsync("TargetLocation", reader).ConfigureAwait(false), out targetLocation))
            {
                this.TargetLocation = targetLocation;
            }

            this.ServiceRequestID = await CommonUtility.ReadElementAsStringAsync("ServiceRequestID", reader).ConfigureAwait(false);
            this.ContentMd5 = await CommonUtility.ReadElementAsStringAsync("ContentMd5", reader).ConfigureAwait(false);
            this.ContentCrc64 = await CommonUtility.ReadElementAsStringAsync("ContentCrc64", reader).ConfigureAwait(false);
            this.Etag = await CommonUtility.ReadElementAsStringAsync("Etag", reader).ConfigureAwait(false);
            this.RequestDate = await CommonUtility.ReadElementAsStringAsync("RequestDate", reader).ConfigureAwait(false);
            try
            {
                this.ErrorCode = await CommonUtility.ReadElementAsStringAsync("ErrorCode", reader).ConfigureAwait(false);
            }
            catch (XmlException)
            {
                /* The ErrorCode property only exists after service version 07-17. 
                 * If it is not present, we are reading an old version and can ignore this property.
                 */
            }
#if WINDOWS_RT || NETCORE
            this.StartTime = DateTimeOffset.Parse(await CommonUtility.ReadElementAsStringAsync("StartTime", reader).ConfigureAwait(false), CultureInfo.InvariantCulture);
            this.EndTime = DateTimeOffset.Parse(await CommonUtility.ReadElementAsStringAsync("EndTime", reader).ConfigureAwait(false), CultureInfo.InvariantCulture);
#else
            this.StartTime = DateTime.Parse(await CommonUtility.ReadElementAsStringAsync("StartTime", reader).ConfigureAwait(false), CultureInfo.InvariantCulture);
            this.EndTime = DateTime.Parse(await CommonUtility.ReadElementAsStringAsync("EndTime", reader).ConfigureAwait(false), CultureInfo.InvariantCulture);
#endif
            this.ExtendedErrorInformation = new StorageExtendedErrorInformation();
            await this.ExtendedErrorInformation.ReadXmlAsync(reader, CancellationToken.None).ConfigureAwait(false);

#if WINDOWS_RT || NETCORE
            this.ExceptionInfo = await ExceptionInfo.ReadFromXMLReaderAsync(reader).ConfigureAwait(false);
#endif
            // End request Result
            await reader.ReadEndElementAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Converts a serializable RequestResult into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter"/> stream to which the RequestResult is serialized.</param>
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "TranslateFromExceptionMessage", Justification = "TranslateFromException is a field name that when split could confuse the users")]
        [SuppressMessage("Microsoft.Naming", "CA2204:Literals should be spelled correctly", MessageId = "RequestResult", Justification = "RequestResult is a variable name which when split could confuse the users")]
        [SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "System.Xml.XmlWriter.WriteComment(System.String)", Justification = "Reviewed. Literals can be used in an internal method")]
#if WINDOWS_RT
        internal
#else
        public
#endif
        void WriteXml(XmlWriter writer)
        {
            CommonUtility.AssertNotNull("writer", writer);

            writer.WriteComment(SR.ExceptionOccurred);
            writer.WriteStartElement("RequestResult");
            writer.WriteElementString("HTTPStatusCode", Convert.ToString(this.HttpStatusCode, CultureInfo.InvariantCulture));
            writer.WriteElementString("HttpStatusMessage", this.HttpStatusMessage);
            writer.WriteElementString("TargetLocation", this.TargetLocation.ToString());

            // Headers
            writer.WriteElementString("ServiceRequestID", this.ServiceRequestID);
            writer.WriteElementString("ContentMd5", this.ContentMd5);
            writer.WriteElementString("ContentCrc64", this.ContentCrc64);
            writer.WriteElementString("Etag", this.Etag);
            writer.WriteElementString("RequestDate", this.RequestDate);
            writer.WriteElementString("ErrorCode", this.ErrorCode);

            // Dates - using RFC 1123 pattern
            writer.WriteElementString("StartTime", this.StartTime.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture));
            writer.WriteElementString("EndTime", this.EndTime.ToUniversalTime().ToString("R", CultureInfo.InvariantCulture));

            // Extended info
            if (this.ExtendedErrorInformation != null)
            {
                this.ExtendedErrorInformation.WriteXml(writer);
            }
            else
            {
                // Write empty
                writer.WriteStartElement(Constants.ErrorRootElement);
                writer.WriteFullEndElement();
            }

#if WINDOWS_RT || NETCORE
            // Exception
            if (this.ExceptionInfo != null)
            {
                this.ExceptionInfo.WriteXml(writer);
            }
            else
            {
                // Write empty
                writer.WriteStartElement("ExceptionInfo");
                writer.WriteFullEndElement();
            }
#endif

            // End RequestResult
            writer.WriteEndElement();
        }
        #endregion
    }
}
