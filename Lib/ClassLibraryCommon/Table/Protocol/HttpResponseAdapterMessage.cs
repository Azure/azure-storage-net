// -----------------------------------------------------------------------------------------
// <copyright file="HttpResponseAdapterMessage.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Table.Protocol
{
    using Microsoft.Data.OData;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Threading.Tasks;

    internal class HttpResponseAdapterMessage : IODataResponseMessage
    {
        private HttpWebResponse resp = null;
        private Stream str = null;
        private string responseContentType = null;

        public HttpResponseAdapterMessage(HttpWebResponse resp, Stream str)
            : this(resp, str, null /* responseContentType */)
        {
        }

        public HttpResponseAdapterMessage(HttpWebResponse resp, Stream str, string responseContentType)
        {
            this.resp = resp;
            this.str = str;
            this.responseContentType = responseContentType;
        }

        public Task<Stream> GetStreamAsync()
        {
            return Task.Factory.StartNew(() => this.str);
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
                    return this.resp.ContentType;
                }
            }

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
            if (headerName == "Content-Encoding")
            {
                return this.resp.ContentEncoding;
            }
#endif

            return this.resp.Headers[headerName];
        }

        public Stream GetStream()
        {
            return this.str;
        }

        public IEnumerable<KeyValuePair<string, string>> Headers
        {
            get
            {
                List<KeyValuePair<string, string>> retHeaders = new List<KeyValuePair<string, string>>();

                foreach (string key in this.resp.Headers.AllKeys)
                {
                    if (key == "Content-Type" && this.responseContentType != null)
                    {
                        retHeaders.Add(new KeyValuePair<string, string>(key, this.responseContentType));
                    }
                    else
                    {
                        retHeaders.Add(new KeyValuePair<string, string>(key, this.resp.Headers[key]));
                    }
                }

                return retHeaders;
            }
        }

        public void SetHeader(string headerName, string headerValue)
        {
            throw new NotImplementedException();
        }

        public int StatusCode
        {
            get
            {
                return (int)this.resp.StatusCode;
            }

            set
            {
                throw new NotSupportedException();
            }
        }
    }
}
