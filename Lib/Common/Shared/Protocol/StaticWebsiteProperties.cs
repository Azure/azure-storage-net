// -----------------------------------------------------------------------------------------
// <copyright file="StaticWebsiteProperties.cs" company="Microsoft">
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
    /// <summary>
    /// Class representing the service properties pertaining to StaticWebsites
    /// </summary>
    public sealed class StaticWebsiteProperties
    {
        /// <summary>
        /// Initializes a new instance of the StaticWebsiteProperties class.
        /// </summary>
        /// <remarks>"Enabled" defaults to false.</remarks>
        public StaticWebsiteProperties()
        {
            this.Enabled = false;
        }

        /// <summary>
        /// True if static websites should be enabled on the blob service for the corresponding Storage Account.
        /// </summary>
        public bool Enabled
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a string representing the name of the index document in each directory.
        /// </summary>
        /// <remarks>This is commonly "index.html".</remarks>
        public string IndexDocument
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a string representing the path to the error document that should be shown when a 404 is issued
        /// (meaning, when a browser requests a page that does not exist.)
        /// </summary>
        /// <example>path/to/error/404.html</example>
        public string ErrorDocument404Path
        {
            get;
            set;
        }
    }
}
