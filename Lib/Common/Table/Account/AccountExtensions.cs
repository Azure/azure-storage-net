using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Table;
using Microsoft.WindowsAzure.Storage.Core;
using System;

namespace Microsoft.WindowsAzure.Storage.Table
{
    internal static class AccountExtensions
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
