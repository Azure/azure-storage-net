﻿//-----------------------------------------------------------------------
// <copyright file="LocationMode.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.RetryPolicies
{
    /// <summary>
    /// Specifies the location mode to indicate which location should receive the request.
    /// </summary>
    public enum LocationMode
    {
        /// <summary>
        /// Requests are always sent to the primary location.
        /// </summary>
        /// <remarks>
        /// If this value is used for requests that only work against a secondary location
        /// (GetServiceStats, for example), the request will fail in the client.
        /// </remarks>
        PrimaryOnly,

        /// <summary>
        /// Requests are always sent to the primary location first. If a request fails, it is sent to the secondary location.
        /// </summary>
        /// <remarks>
        /// If this value is used for requests that are only valid against one location, the client will
        /// only target the allowed location.
        /// </remarks>
        PrimaryThenSecondary,

        /// <summary>
        /// Requests are always sent to the secondary location.
        /// </summary>
        /// <remarks>
        /// If this value is used for requests that only work against a primary location
        /// (create, modify, and delete APIs), the request will fail in the client.
        /// </remarks>
        SecondaryOnly,

        /// <summary>
        /// Requests are always sent to the secondary location first. If a request fails, it is sent to the primary location.
        /// </summary>
        /// <remarks>
        /// If this value is used for requests that are only valid against one location, the client will
        /// only target the allowed location.
        /// </remarks>
        SecondaryThenPrimary,
    }
}
