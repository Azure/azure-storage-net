//-----------------------------------------------------------------------
// <copyright file="UriQueryBuilder.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// A convenience class for constructing URI query strings.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif    
    class UriQueryBuilder
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="UriQueryBuilder"/> class.
        /// </summary>
        public UriQueryBuilder()
            : this(null /* builder */)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UriQueryBuilder"/> class that contains elements copied from the specified <see cref="UriQueryBuilder"/>.
        /// </summary>
        /// <param name="builder">The <see cref="UriQueryBuilder"/> whose elements are copied to the new <see cref="UriQueryBuilder"/>.</param>
        public UriQueryBuilder(UriQueryBuilder builder)
        {
            this.Parameters = builder != null ?
                new Dictionary<string, string>(builder.Parameters) :
                new Dictionary<string, string>();
        }
        
        /// <summary>
        /// Stores the query parameters.
        /// </summary>
        protected IDictionary<string, string> Parameters { get; private set; }

        /// <summary>
        /// Gets the query string value associated with the given name.
        /// </summary>
        /// <param name="name">The query string name.</param>
        public string this[string name]
        {
            get
            {
                string value;
                if (this.Parameters.TryGetValue(name, out value))
                {
                    return value;
                }
                else
                {
                    throw new KeyNotFoundException(string.Format(CultureInfo.InvariantCulture, SR.QueryBuilderKeyNotFound, name));
                }
            }
        }

        /// <summary>
        /// Add the query string value with URI escaping.
        /// </summary>
        /// <param name="name">The query string name.</param>
        /// <param name="value">The query string value.</param>
        public virtual void Add(string name, string value)
        {
            if (value != null)
            {
                value = Uri.EscapeDataString(value);
            }

            this.Parameters.Add(name, value);
        }

        /// <summary>
        /// Add multiple query string values with URI escaping.
        /// </summary>
        /// <param name="parameters">The set of query string name/value pairs</param>
        public void AddRange(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            CommonUtility.AssertNotNull("parameters", parameters);

            foreach (KeyValuePair<string, string> parameter in parameters)
            {
                this.Add(parameter.Key, parameter.Value);
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> containing the URI.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> containing the URI.
        /// </returns>
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            bool first = true;

            foreach (KeyValuePair<string, string> pair in this.Parameters)
            {
                if (first)
                {
                    first = false;
                    sb.Append("?");
                }
                else
                {
                    sb.Append("&");
                }

                sb.Append(pair.Key);

                if (pair.Value != null)
                {
                    sb.AppendFormat("={0}", pair.Value);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Adds a query parameter to a URI.
        /// </summary>
        /// <param name="storageUri">A <see cref="StorageUri"/> containing the original URI, including any existing query parameters.</param>
        /// <returns>A <see cref="StorageUri"/> object with the new query parameter appended.</returns>
        public StorageUri AddToUri(StorageUri storageUri)
        {
            CommonUtility.AssertNotNull("storageUri", storageUri);

            return new StorageUri(
                this.AddToUri(storageUri.PrimaryUri),
                this.AddToUri(storageUri.SecondaryUri));
        }

        /// <summary>
        /// Adds a query parameter to a URI.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> object containing the original URI, including any existing query parameters.</param>
        /// <returns>A <see cref="System.Uri"/> object with the new query parameter appended.</returns>
        public virtual Uri AddToUri(Uri uri)
        {
            return this.AddToUriCore(uri);
        }

        /// <summary>
        /// Adds a query parameter to a URI.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> object containing the original URI, including any existing query parameters.</param>
        /// <returns>A <see cref="System.Uri"/> object with the new query parameter appended.</returns>
        protected Uri AddToUriCore(Uri uri)
        {
            if (uri == null)
            {
                return null;
            }

            // The correct way to add query parameters to a URI http://msdn.microsoft.com/en-us/library/system.uribuilder.query.aspx
            string queryToAppend = this.ToString();

            if (queryToAppend.Length > 1)
            {
                queryToAppend = queryToAppend.Substring(1);
            }

            UriBuilder baseUri = new UriBuilder(uri);

            if (baseUri.Query != null && baseUri.Query.Length > 1)
            {
                baseUri.Query = baseUri.Query.Substring(1) + "&" + queryToAppend;
            }
            else
            {
                baseUri.Query = queryToAppend;
            }

            return baseUri.Uri;
        }
    }
}
