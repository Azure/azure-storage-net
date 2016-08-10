//-----------------------------------------------------------------------
// <copyright file="SasQueryBuilder.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;

    /// <summary>
    /// A convenience class for constructing SAS-specific URI query strings.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif   
    class SasQueryBuilder : UriQueryBuilder
    {
        /// <summary>
        /// Public SasQueryBuilder constructor.
        /// </summary>
        /// <param name="sasToken">The ASA token used to authenticate request.</param>
        public SasQueryBuilder(string sasToken)
        {
            this.AddRange(HttpWebUtility.ParseQueryString(sasToken));
        }

        /// <summary>
        /// Returns True if any of the parameters specifies https:.
        /// </summary>
        public bool RequireHttps { get; private set; }

        /// <summary>
        /// Add the query string value with URI escaping.
        /// </summary>
        /// <param name="name">The query string name.</param>
        /// <param name="value">The query string value.</param>
        public override void Add(string name, string value)
        {
            if (value != null)
            {
                value = Uri.EscapeDataString(value);
            }

            if (string.CompareOrdinal(name, Constants.QueryConstants.SignedProtocols) == 0 && string.CompareOrdinal(value, "https") == 0)
            {
                this.RequireHttps = true;
            }

            this.Parameters.Add(name, value);
        }

        /// <summary>
        /// Adds a query parameter to a URI.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> object containing the original URI, including any existing query parameters.</param>
        /// <returns>A <see cref="System.Uri"/> object with the new query parameter appended.</returns>
        public override Uri AddToUri(Uri uri)
        {
            CommonUtility.AssertNotNull("uri", uri);

#if WINDOWS_RT   || NETCORE
            if (this.RequireHttps && (string.CompareOrdinal(uri.Scheme, "https") != 0))
#else
            if (this.RequireHttps && (string.CompareOrdinal(uri.Scheme, Uri.UriSchemeHttps) != 0))
#endif
            {
                throw new ArgumentException(SR.CannotTransformNonHttpsUriWithHttpsOnlyCredentials);
            }

            return this.AddToUriCore(uri);
        }
    }
}
