// -----------------------------------------------------------------------------------------
// <copyright file="AccessPolicyResponseBase.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
    using Core;
    using Core.Util;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    /// Parses the response XML from an operation to set the access policy for a cloud object.
    /// </summary>
    /// <typeparam name="T">The policy type to be filled.</typeparam>
    internal abstract class AccessPolicyResponseBase<T>: IDisposable
        where T : new()
    {
        /// <summary>
        /// The reader used for parsing. This field is reserved and should not be used.
        /// </summary>
        [SuppressMessage(
            "Microsoft.StyleCop.CSharp.MaintainabilityRules",
            "SA1401:FieldsMustBePrivate",
            Justification = "Unable to change while remaining backwards compatible.")]
        protected XmlReader reader;

        /// <summary>
        /// Initializes a new instance of the AccessPolicyResponseBase class.
        /// </summary>
        /// <param name="stream">The stream to be parsed.</param>
        protected AccessPolicyResponseBase(Stream stream)
        {
            this.reader = XMLReaderExtensions.CreateAsAsync(stream);
            this.AccessIdentifiers = this.ParseAsync();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. 
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources, and optional
        /// managed resources.
        /// </summary>
        /// <param name="disposing"><c>True</c> to release both managed and unmanaged resources; otherwise, <c>false</c>.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.reader != null)
                {
#if WINDOWS_RT || NETCORE
                    this.reader.Dispose();
#else
                    this.reader.Close();
#endif
                }
            }

            this.reader = null;
        }

        /// <summary>
        /// Gets an enumerable collection of container-level access policy identifiers.
        /// </summary>
        /// <value>An enumerable collection of container-level access policy identifiers.</value>
        public Task<IEnumerable<KeyValuePair<string, T>>> AccessIdentifiers { get; private set; }

        /// <summary>
        /// Parses the current element.
        /// </summary>
        /// <param name="accessPolicyElement">The shared access policy element to parse.</param>
        /// <returns>The shared access policy.</returns>
        protected abstract T ParseElement(XElement accessPolicyElement);

        /// <summary>
        /// Parses the response XML from a Set Container ACL operation to retrieve container-level access policy data.
        /// </summary>
        /// <returns>A list of enumerable key-value pairs.</returns>
        protected Task<IEnumerable<KeyValuePair<string, T>>> ParseAsync()
        {
            return Task.Run(
                ()
                =>
                {
                    var result = new List<KeyValuePair<string, T>>();

                    XElement root = XElement.Load(reader);
                    IEnumerable<XElement> elements = root.Elements(Constants.SignedIdentifier);
                    foreach (XElement signedIdentifierElement in elements)
                    {
                        string id = (string)signedIdentifierElement.Element(Constants.Id);
                        T accessPolicy;
                        XElement accessPolicyElement = signedIdentifierElement.Element(Constants.AccessPolicy);
                        if (accessPolicyElement != null)
                        {
                            accessPolicy = this.ParseElement(accessPolicyElement);
                        }
                        else
                        {
                            accessPolicy = new T();
                        }

                        result.Add(new KeyValuePair<string, T>(id, accessPolicy));
                    }


#if WINDOWS_RT || NETCORE
                    this.reader.Dispose();
#else
                    this.reader.Close();
#endif
                    this.reader = null;

                    return result.AsEnumerable();
                });
        }
    }
}
