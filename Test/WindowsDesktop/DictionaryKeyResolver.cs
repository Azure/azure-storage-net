using Microsoft.Azure.KeyVault.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage
{
    public class DictionaryKeyResolver : IKeyResolver
    {
        private Dictionary<string, IKey> keys = new Dictionary<string, IKey>();

        public void Add(IKey key)
        {
            keys[key.Kid] = key;
        }

#pragma warning disable 1998
        public async Task<IKey> ResolveKeyAsync(string kid, CancellationToken token)
        {
            IKey result;
            keys.TryGetValue(kid, out result);
            return result;
        }
#pragma warning restore 1998

    }
}
