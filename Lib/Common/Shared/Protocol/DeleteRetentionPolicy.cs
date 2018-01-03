// -----------------------------------------------------------------------------------------
// <copyright file="CorsRule.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{
    /// <summary>
    /// Class representing the service properties pertaining to DeleteRetentionPolicy
    /// </summary>
    public sealed class DeleteRetentionPolicy
    {
        /// <summary>
        /// Gets or sets the Enabled flag of the DeleteRetentionPolicy.
        /// </summary>
        /// <value>Indicates whether DeleteRetentionPolicy is enabled for the Blob service. </value>
        public bool Enabled
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or Sets the number of days on the DeleteRetentionPolicy.
        /// </summary>
        /// <value>Indicates the number of days that the deleted blob should be retained. The minimum specified value can be 1 and the maximum value can be 365. </value>
        public int? RetentionDays
        {
            get;
            set;
        }
    }
}
