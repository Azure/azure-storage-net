// -----------------------------------------------------------------------------------------
// <copyright file="DefaultSerializationSettings.cs" company="Microsoft">
//    Copyright 2018 Microsoft Corporation
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

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.WindowsAzure.Storage.Table.Protocol
{
    static class DefaultSerializer
    {
        public static JsonSerializer Create()
        {
            return new JsonSerializer
            {
                ContractResolver = new DefaultContractResolver()
            };
        }
    }
    static class DefaultSerializerSettings
    {
        public static JsonSerializerSettings Create()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver()
            };
        }
    }
}