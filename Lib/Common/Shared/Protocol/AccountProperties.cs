// -----------------------------------------------------------------------------------------
// <copyright file="ServiceProperties.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;

    /// <summary>
    /// Class representing a set of properties pertaining to a cloud storage account.
    /// </summary>
    public sealed class AccountProperties
    {
        /// <summary>
        /// Initializes a new instance of the ServiceProperties class.
        /// </summary>
        public AccountProperties()
        {
        }

        /// <summary>
        /// Gets the account SKU type based on GeoReplication state.
        /// </summary>
        /// <value>"Standard_LRS", "Standard_ZRS", "Standard_GRS", "Standard_RAGRS", "Premium_LRS", or "Premium_ZRS"</value>
        public string SkuName
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the account kind.
        /// </summary>
        /// <value>"Storage", "StorageV2", or "BlobStorage"</value>
        public string AccountKind
        {
            get;
            private set;
        }
        
        /// <summary>
        /// Constructs an <c>AccountProperties</c> object from a HttpResponseHeaders object received from the service.
        /// </summary>
        /// <param name="httpResponseHeaders">The HttpResponseHeaders object.</param>
        /// <returns>An <c>AccountProperties</c> object containing the properties in the HttpResponseHeaders.</returns>
        internal static AccountProperties FromHttpResponseHeaders(HttpResponseHeaders httpResponseHeaders)
        {
            IEnumerable<string> values;

            AccountProperties properties = new AccountProperties
            {
                SkuName = httpResponseHeaders.TryGetValues(Constants.HeaderConstants.SkuNameName, out values) ? values.Single() : null,
                AccountKind = httpResponseHeaders.TryGetValues(Constants.HeaderConstants.AccountKindName, out values) ? values.Single() : null,
            };

            return properties;
        }
    }
}
