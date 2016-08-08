//-----------------------------------------------------------------------
// <copyright file="EncryptionAgent.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage
{
#if !(WINDOWS_RT || NETCORE)
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
#endif

    /// <summary>
    /// Represents the encryption agent stored on the service. It consists of the encryption protocol version and encryption algorithm used.
    /// </summary>
    internal sealed class EncryptionAgent
    {
        /// <summary>
        /// The protocol version used for encryption.
        /// </summary>
        public string Protocol { get; set; }

        /// <summary>
        /// The algorithm used for encryption.
        /// </summary>
#if !(WINDOWS_RT || NETCORE)
        [JsonConverter(typeof(StringEnumConverter))]
#endif
        public EncryptionAlgorithm EncryptionAlgorithm { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="EncryptionAgent"/> class using the specified protocol version and the algorithm. 
        /// </summary>
        /// <param name="protocol">The encryption protocol version.</param>
        /// <param name="algorithm">The encryption algorithm.</param>
        public EncryptionAgent(string protocol, EncryptionAlgorithm algorithm)
        {
            this.Protocol = protocol;
            this.EncryptionAlgorithm = algorithm;
        }
    }
}
