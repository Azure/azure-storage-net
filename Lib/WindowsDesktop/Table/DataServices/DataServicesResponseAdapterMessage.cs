// -----------------------------------------------------------------------------------------
// <copyright file="DataServicesResponseAdapterMessage.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Table.DataServices
{
    using Microsoft.Data.OData;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    internal class DataServicesResponseAdapterMessage : IODataResponseMessage
    {
        private IDictionary<string, string> responseHeaders;
        private Stream inputStream = null;
        private string responseContentType = null;

        public DataServicesResponseAdapterMessage(Dictionary<string, string> responseHeaders, Stream inputStream)
            : this(responseHeaders, inputStream, null /* responseContentType */)
        {
        }

        public DataServicesResponseAdapterMessage(IDictionary<string, string> responseHeaders, Stream inputStream, string responseContentType)
        {
            this.responseHeaders = responseHeaders;
            this.inputStream = inputStream;
            this.responseContentType = responseContentType;
        }

        public Task<Stream> GetStreamAsync()
        {
            return Task.Factory.StartNew(() => this.inputStream);
        }

        public string GetHeader(string headerName)
        {
            if (headerName == "Content-Type")
            {
                if (this.responseContentType != null)
                {
                    return this.responseContentType;
                }
                else
                {
                    string value = this.responseHeaders["Content-Type"];
                    return value;
                }
            }
            else if (headerName == "DataServiceVersion" || headerName == "Preference-Applied")
            {
                return null;
            }

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
            if (headerName == "Content-Encoding")
            {
                return this.responseHeaders["ContentEncoding"];
            }
#endif

            return this.responseHeaders[headerName];
        }

        public Stream GetStream()
        {
            return this.inputStream;
        }

        public IEnumerable<KeyValuePair<string, string>> Headers
        {
            get
            {
                List<KeyValuePair<string, string>> retHeaders = new List<KeyValuePair<string, string>>();

                foreach (string key in this.responseHeaders.Keys)
                {
                    if (key == "Content-Type" && this.responseContentType != null)
                    {
                        retHeaders.Add(new KeyValuePair<string, string>(key, this.responseContentType));
                    }
                    else
                    {
                        retHeaders.Add(new KeyValuePair<string, string>(key, this.responseHeaders[key]));
                    }
                }

                return retHeaders;
            }
        }

        public void SetHeader(string headerName, string headerValue)
        {
            throw new NotSupportedException();
        }

        public int StatusCode
        {
            get
            {
                throw new NotSupportedException();
            }

            set
            {
                throw new NotSupportedException();
            }
        }
    }
}
