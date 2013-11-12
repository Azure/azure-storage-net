// -----------------------------------------------------------------------------------------
// <copyright file="CorsRule.cs" company="Microsoft">
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

    /// <summary>
    /// Class representing the service properties pertaining to CORS.
    /// </summary>
    public sealed class CorsRule
    {
        private IList<string> allowedOrigins;
        private IList<string> exposedHeaders;
        private IList<string> allowedHeaders;
        
        /// <summary>
        /// Gets or sets domain names allowed via CORS.
        /// </summary>
        /// <value>A collection of strings containing the allowed domain names, limited to 64.</value>
        public IList<string> AllowedOrigins
        {
            get
            {
                return this.allowedOrigins ?? (this.allowedOrigins = new List<string>());
            }

            set
            {
                this.allowedOrigins = value;
            }
        }

        /// <summary>
        /// Gets or sets response headers that should be exposed to client via CORS.
        /// </summary>
        /// <value>A collection of strings containing exposed headers, limited to 64 defined headers and two prefixed headers.</value>
        public IList<string> ExposedHeaders
        {
            get
            {
                return this.exposedHeaders ?? (this.exposedHeaders = new List<string>());
            }

            set
            {
                this.exposedHeaders = value;
            }
        }

        /// <summary>
        /// Gets or sets headers allowed to be part of the CORS request.
        /// </summary>
        /// <value>A collection of strings containing allowed headers, limited to 64 defined headers and two prefixed headers.</value>
        public IList<string> AllowedHeaders
        {
            get
            {
                return this.allowedHeaders ?? (this.allowedHeaders = new List<string>());
            }

            set
            {
                this.allowedHeaders = value;
            }
        }

        /// <summary>
        /// Gets or sets the HTTP methods permitted to execute for this origin.
        /// </summary>
        /// <value>The allowed HTTP methods.</value>
        public CorsHttpMethods AllowedMethods
        {
            get; 
            set; 
        }

        /// <summary>
        /// Gets or sets the length of time in seconds that a preflight response should be cached by browser.
        /// </summary>
        /// <value>The maximum number of seconds to cache the response.</value>
        public int MaxAgeInSeconds
        {
            get;
            set;
        }
    }
}
