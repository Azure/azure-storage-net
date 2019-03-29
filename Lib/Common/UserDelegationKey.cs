// -----------------------------------------------------------------------------------------
// <copyright file="UserDelegationKey.cs" company="Microsoft">
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

using System;

namespace Microsoft.Azure.Storage
{
    /// <summary>
    /// Represents a user delegation key, provided to the user by Azure Storage
    /// based on their Azure Active Directory access token.
    /// </summary>
    public sealed class UserDelegationKey
    {
        /// <summary>
        /// Object ID of this token.
        /// </summary>
        /// <value>A <see cref="Guid"/> representing the object ID.</value>
        public Guid? SignedOid { get; internal set; }

        /// <summary>
        /// Tenant ID of the tenant that issued this token.
        /// </summary>
        /// <value>A <see cref="Guid"/> representing the tenant ID.</value>
        public Guid? SignedTid { get; internal set; }

        /// <summary>
        /// The datetime this token becomes valid.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> representing the time.</value>
        public DateTimeOffset? SignedStart { get; internal set; }

        /// <summary>
        /// The datetime this token expires.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> representing the time.</value>
        public DateTimeOffset? SignedExpiry { get; internal set; }

        /// <summary>
        /// What service this key is valid for.
        /// </summary>
        /// <value>The REST service's abbreviation of the service type.</value>
        public string SignedService { get; internal set; }

        /// <summary>
        /// The version identifier of the REST service that created this token.
        /// </summary>
        /// <value>A <see cref="string"/> identifying the version.</value>
        public string SignedVersion { get; internal set; }

        /// <summary>
        /// The user delegation key.
        /// </summary>
        /// <value>A <see cref="string"/> representing the key in base 64.</value>
        public string Value { get; internal set; }
    }
}