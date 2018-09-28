// -----------------------------------------------------------------------------------------
// <copyright file="Response.cs" company="Microsoft">
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
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    internal static class Response
    {

        /// <summary>
        /// Reads a collection of shared access policies from the specified <see cref="AccessPolicyResponseBase&lt;T&gt;"/> object.
        /// </summary>
        /// <param name="sharedAccessPolicies">A collection of shared access policies to be filled.</param>
        /// <param name="policyResponse">A policy response object for reading the stream.</param>
        /// <typeparam name="T">The type of policy to read.</typeparam>
        internal static async Task ReadSharedAccessIdentifiersAsync<T>(IDictionary<string, T> sharedAccessPolicies, AccessPolicyResponseBase<T> policyResponse, CancellationToken token)
            where T : new()
        {
            token.ThrowIfCancellationRequested();

            foreach (KeyValuePair<string, T> pair in await policyResponse.AccessIdentifiers.ConfigureAwait(false))
            {
                sharedAccessPolicies.Add(pair.Key, pair.Value);
            }
        }

        /// <summary>
        /// Parses the metadata.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>A <see cref="System.Collections.IDictionary"/> of metadata.</returns>
        /// <remarks>
        /// Precondition: reader at &lt;Metadata&gt;
        /// Postcondition: reader after &lt;/Metadata&gt; (&lt;Metadata/&gt; consumed)
        /// </remarks>
        internal static IDictionary<string, string> ParseMetadata(XmlReader reader)
        {
            IDictionary<string, string> metadata = new Dictionary<string, string>();
            bool needToRead = true;
            while (true)
            {
                if (needToRead && !reader.Read())
                {
                    return metadata;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    needToRead = false;

                    if (reader.IsEmptyElement)
                    {
                        reader.Read();
                        continue;
                    }

                    string elementName = reader.Name;
                    string elementValue = reader.ReadElementContentAsString();
                    if (elementName != Constants.InvalidMetadataName)
                    {
                        metadata.Add(elementName, elementValue);
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == Constants.MetadataElement)
                {
                    reader.Read();
                    return metadata;
                }
            }
        }

        /// <summary>
        /// Parses the metadata.
        /// </summary>
        /// <param name="reader">The reader.</param>
        /// <returns>A <see cref="System.Collections.IDictionary"/> of metadata.</returns>
        /// <remarks>
        /// Precondition: reader at &lt;Metadata&gt;
        /// Postcondition: reader after &lt;/Metadata&gt; (&lt;Metadata/&gt; consumed)
        /// </remarks>
        internal static async Task<IDictionary<string, string>> ParseMetadataAsync(XmlReader reader)
        {
            IDictionary<string, string> metadata = new Dictionary<string, string>();
            bool needToRead = true;
            while (true)
            {
                if (needToRead && !(await reader.ReadAsync().ConfigureAwait(false)))
                {
                    return metadata;
                }

                if (reader.NodeType == XmlNodeType.Element)
                {
                    needToRead = false;

                    if (reader.IsEmptyElement)
                    {
                        await reader.ReadAsync().ConfigureAwait(false);
                        continue;
                    }

                    string elementName = reader.Name;
                    string elementValue = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                    if (elementName != Constants.InvalidMetadataName)
                    {
                        metadata.Add(elementName, elementValue);
                    }
                }
                else if (reader.NodeType == XmlNodeType.EndElement && reader.Name == Constants.MetadataElement)
                {
                    await reader.ReadAsync().ConfigureAwait(false);
                    return metadata;
                }
            }
        }
    }
}
