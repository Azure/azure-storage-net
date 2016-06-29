// -----------------------------------------------------------------------------------------
// <copyright file="QueueContinuationToken.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    /// <summary>
    /// Represents a continuation token returned by the Queue service.
    /// </summary>
#if WINDOWS_DESKTOP && !WINDOWS_PHONE
    [Serializable]

    // If an XmlSerializer is used rather than directly calling ReadXml() or WriteXml(), it will write this as the root token when serializing, 
    // and assume to see this as the root token when deserializing
    [XmlRoot(Constants.ContinuationConstants.ContinuationTopElement, IsNullable = false)]
#endif
    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1001:CommasMustBeSpacedCorrectly", Justification = "Reviewed.")]
    public sealed class QueueContinuationToken : IContinuationToken
#if WINDOWS_DESKTOP
, IXmlSerializable
#endif
    {
        private string version = Constants.ContinuationConstants.CurrentVersion;
        private string type = Constants.ContinuationConstants.QueueType;

        /// <summary>
        /// Gets or sets the version for continuing results for CloudQueue enumeration operations.
        /// </summary>
        /// <value>The version.</value>
        private string Version
        {
            get
            {
                return this.version;
            }

            set
            {
                this.version = value;
                if (this.version != Constants.ContinuationConstants.CurrentVersion)
                {
                    throw new XmlException(string.Format(CultureInfo.InvariantCulture, SR.UnexpectedElement, this.version));
                }
            }
        }

        /// <summary>
        /// Gets or sets the type element (blob, queue, table, file) for continuing results for CloudQueue enumeration operations.
        /// </summary>
        /// <value>The type element.</value>
        private string Type
        {
            get
            {
                return this.type;
            }

            set
            {
                this.type = value;
                if (this.type != Constants.ContinuationConstants.QueueType)
                {
                    throw new XmlException(SR.UnexpectedContinuationType);
                }
            }
        }

        /// <summary>
        /// Gets or sets the next marker for continuing results for <see cref="CloudQueue"/> enumeration operations.
        /// </summary>
        /// <value>A string containing the NextMarker value.</value>
        public string NextMarker { get; set; }

        /// <summary>
        /// Gets or sets the storage location that the continuation token applies to.
        /// </summary>
        /// <value>A <see cref="StorageLocation"/> enumeration value.</value>
        public StorageLocation? TargetLocation { get; set; }

#if WINDOWS_DESKTOP
        /// <summary>
        /// Gets an XML representation of an object.
        /// </summary>
        /// <returns>
        /// An <see cref="T:System.Xml.Schema.XmlSchema"/> that describes the XML representation of the object that is produced by the <see cref="M:System.Xml.Serialization.IXmlSerializable.WriteXml(System.Xml.XmlWriter)"/> method and consumed by the <see cref="M:System.Xml.Serialization.IXmlSerializable.ReadXml(System.Xml.XmlReader)"/> method.
        /// </returns>
        public XmlSchema GetSchema()
        {
            return null;
        }

        /// <summary>
        /// Generates a serializable continuation token from its XML representation.
        /// </summary>
        /// <param name="reader">The <see cref="T:System.Xml.XmlReader"/> stream from which the continuation token is deserialized.</param>
        public void ReadXml(XmlReader reader)
        {
            CommonUtility.AssertNotNull("reader", reader);

            // Read the xml root node
            reader.MoveToContent();
            reader.ReadStartElement();

            // Read the additional xml node that will be present if an XmlSerializer was used to serialize this token
            reader.MoveToContent();
            if (reader.Name == Constants.ContinuationConstants.ContinuationTopElement)
            {
                reader.ReadStartElement();
            }

            // Read the ContinuationToken content
            while (reader.IsStartElement())
            {
                switch (reader.Name)
                {
                    case Constants.ContinuationConstants.VersionElement:
                        this.Version = reader.ReadElementContentAsString();
                        break;

                    case Constants.ContinuationConstants.NextMarkerElement:
                        this.NextMarker = reader.ReadElementContentAsString();
                        break;

                    case Constants.ContinuationConstants.TargetLocationElement:
                        string targetLocation = reader.ReadElementContentAsString();
                        StorageLocation location;
                        if (Enum.TryParse(targetLocation, out location))
                        {
                            this.TargetLocation = location;
                        }
                        else if (!string.IsNullOrEmpty(targetLocation)) 
                        {
                            throw new XmlException(string.Format(CultureInfo.InvariantCulture, SR.UnexpectedLocation, targetLocation));
                        }

                        break;

                    case Constants.ContinuationConstants.TypeElement:
                        this.Type = reader.ReadElementContentAsString();
                        break;

                    default:
                        throw new XmlException(string.Format(CultureInfo.InvariantCulture, SR.UnexpectedElement, reader.Name));
                }
            }
        }

        /// <summary>
        /// Converts a serializable continuation token into its XML representation.
        /// </summary>
        /// <param name="writer">The <see cref="T:System.Xml.XmlWriter"/> stream to which the continuation token is serialized.</param>
        public void WriteXml(XmlWriter writer)
        {
            CommonUtility.AssertNotNull("writer", writer);

            writer.WriteStartElement(Constants.ContinuationConstants.ContinuationTopElement);

            writer.WriteElementString(Constants.ContinuationConstants.VersionElement, this.Version);

            writer.WriteElementString(Constants.ContinuationConstants.TypeElement, this.Type);

            if (this.NextMarker != null)
            {
                writer.WriteElementString(Constants.ContinuationConstants.NextMarkerElement, this.NextMarker);
            }

            writer.WriteElementString(Constants.ContinuationConstants.TargetLocationElement, this.TargetLocation.ToString());

            writer.WriteEndElement(); // End ContinuationToken
        }
#endif
    }
}
