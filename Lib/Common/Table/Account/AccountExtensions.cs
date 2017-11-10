using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Table;
using Microsoft.Azure.Storage.Core;
using System;

namespace Microsoft.Azure.Storage.Table
{
    public static class TableAccountExtensions
    {
        /// <summary>
        /// Creates the Table service client.
        /// </summary>
        /// <returns>A <see cref="CloudTableClient"/> object.</returns>
        public static CloudTableClient CreateCloudTableClient(this CloudStorageAccount account)
        {
            if (account.TableEndpoint == null)
            {
                throw new InvalidOperationException(SR.TableEndPointNotConfigured);
            }

            return new CloudTableClient(account.TableStorageUri, account.Credentials);
        }
    }
}
