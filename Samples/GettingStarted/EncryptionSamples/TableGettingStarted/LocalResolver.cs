// <copyright file="LocalResolver.cs" company="Microsoft">
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

namespace TableGettingStartedUsingResolver
{
    using Microsoft.Azure.KeyVault.Core;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    public class LocalResolver : IKeyResolver
    {
        private Dictionary<string, IKey> keys = new Dictionary<string, IKey>();

        public void Add(IKey key)
        {
            keys[key.Kid] = key;
        }

        public async Task<IKey> ResolveKeyAsync(string kid, CancellationToken token)
        {
            IKey result;

            keys.TryGetValue(kid, out result);

            return await Task.FromResult(result);
        }
    }
}
