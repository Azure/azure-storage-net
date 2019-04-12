//-----------------------------------------------------------------------
// <copyright file="PremiumPageBlobTier.cs" company="Microsoft">
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
namespace Microsoft.Azure.Storage.Blob
{
    /// <summary>
    /// The tier of the page blob.
    /// Please take a look at https://docs.microsoft.com/en-us/azure/storage/storage-premium-storage#scalability-and-performance-targets
    /// for detailed information on the corresponding IOPS and throughtput per PremiumPageBlobTier.
    /// </summary>
    public enum PremiumPageBlobTier
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
        P30,

        /// <summary>
        /// P40 Tier
        /// </summary>
        P40,

        /// <summary>
        /// P50 Tier
        /// </summary>
        P50,

        /// <summary>
        /// P60 Tier
        /// </summary>
        P60,

        /// <summary>
        /// P70 Tier
        /// </summary>
        P70,

        /// <summary>
        /// P80 Tier
        /// </summary>
        P80
    }
}