// -----------------------------------------------------------------------------------------
// <copyright file="GetUserDelegationKeyResponse.cs" company="Microsoft">
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

using Microsoft.Azure.Storage.Core.Util;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Microsoft.Azure.Storage.Shared.Protocol
{
    internal static class GetUserDelegationKeyResponse
    {

        internal static async Task<UserDelegationKey> ParseAsync(Stream stream, CancellationToken token)
        {
            UserDelegationKey key = null;
            using (XmlReader reader = XMLReaderExtensions.CreateAsAsync(stream))
            {
                token.ThrowIfCancellationRequested();

                if (await reader.ReadToFollowingAsync(Constants.UserDelegationKey).ConfigureAwait(false))
                {
                    if (reader.IsEmptyElement)
                    {
                        await reader.SkipAsync().ConfigureAwait(false);
                    }
                    else
                    {
                        key = await ParseKey(reader, token);
                    }
                }
            }

            return key;
        }

        private static async Task<UserDelegationKey> ParseKey(XmlReader reader, CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            Guid signedOid = default(Guid);
            Guid signedTid = default(Guid);
            DateTimeOffset signedStart = default(DateTimeOffset);
            DateTimeOffset signedExpiry = default(DateTimeOffset);
            string signedService = default(string);
            string signedVersion = default(string);
            string value = default(string);

            await reader.ReadStartElementAsync().ConfigureAwait(false);
            while (await reader.IsStartElementAsync().ConfigureAwait(false))
            {
                switch (reader.Name)
                {
                    case Constants.SignedOid:
                        signedOid = Guid.Parse(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                        break;
                    case Constants.SignedTid:
                        signedTid = Guid.Parse(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                        break;
                    case Constants.SignedStart:
                        signedStart = DateTimeOffset.Parse(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                        break;
                    case Constants.SignedExpiry:
                        signedExpiry = DateTimeOffset.Parse(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
                        break;
                    case Constants.SignedService:
                        signedService = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        break;
                    case Constants.SignedVersion:
                        signedVersion = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        break;
                    case Constants.Value:
                        value = await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
                        break;
                    default:
                        await reader.SkipAsync().ConfigureAwait(false);
                        break;
                }
            }

            await reader.ReadEndElementAsync().ConfigureAwait(false);

            return new UserDelegationKey()
            {
                SignedOid = signedOid,
                SignedTid = signedTid,
                SignedStart = signedStart,
                SignedExpiry = signedExpiry,
                SignedService = signedService,
                SignedVersion = signedVersion,
                Value = value
            };
        }
    }
}