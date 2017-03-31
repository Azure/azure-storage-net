//-----------------------------------------------------------------------
// <copyright file="PageBlobTier.cs" company="Microsoft">
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

using System;
namespace Microsoft.WindowsAzure.Storage.Blob
{
    /// <summary>
    /// The tier of the page blob.
    /// </summary>
    public enum PageBlobTier
    {
        /// <summary>
        /// The tier is not recognized by this version of the library.
        /// </summary>
        Unknown,

        /// <summary>
        /// P4 Tier
        /// </summary>
        P4,

        /// <summary>
        /// P6 Tier
        /// </summary>
        P6,

        /// <summary>
        /// P10 Tier
        /// </summary>
        P10,

        /// <summary>
        /// P20 Tier
        /// </summary>
        P20,

        /// <summary>
        /// P30 Tier
        /// </summary>
        P30
    }
}