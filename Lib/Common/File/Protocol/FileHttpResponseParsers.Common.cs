//-----------------------------------------------------------------------
// <copyright file="FileHttpResponseParsers.Common.cs" company="Microsoft">
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
// <summary>
//    Contains code for the CloudStorageAccount class.
// </summary>
//-----------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System.IO;
    using System.Xml;
    using System.Xml.Linq;

#if WINDOWS_RT
    internal
#else
    public
#endif
        static partial class FileHttpResponseParsers
    {
        /// <summary>
        /// Reads service properties from a stream.
        /// </summary>
        /// <param name="inputStream">The stream from which to read the service properties.</param>
        /// <returns>The service properties stored in the stream.</returns>
        public static FileServiceProperties ReadServiceProperties(Stream inputStream)
        {
            using (XmlReader reader = XmlReader.Create(inputStream))
            {
                XDocument servicePropertyDocument = XDocument.Load(reader);

                return FileServiceProperties.FromServiceXml(servicePropertyDocument);
            }
        }

        /// <summary>
        /// Reads service stats from a stream.
        /// </summary>
        /// <param name="inputStream">The stream from which to read the service stats.</param>
        /// <returns>The service stats stored in the stream.</returns>
        public static ServiceStats ReadServiceStats(Stream inputStream)
        {
            return HttpResponseParsers.ReadServiceStats(inputStream);
        }
    }
}
