// -----------------------------------------------------------------------------------------
// <copyright file="DictionaryKeyResolver.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage
{
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.KeyVault.Core;

    public class DictionaryKeyResolver : IKeyResolver
    {
        private Dictionary<string, IKey> keys = new Dictionary<string, IKey>();

        public void Add(IKey key)
        {
            keys[key.Kid] = key;
        }

        public Task<IKey> ResolveKeyAsync(string kid, CancellationToken token)
        {
            IKey result;
            keys.TryGetValue(kid, out result);
            return new TaskFactory().StartNew<IKey>(() => result);
        }
    }
}
