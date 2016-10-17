// -----------------------------------------------------------------------------------------
// <copyright file="TableStatus.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Table
{
    /// <summary>
    /// The table status.
    /// </summary>
    public enum TableStatus
    {
        /// <summary>
        /// The table staus is not specified.
        /// </summary>
        Unspecified,

        /// <summary>
        /// Indicates that the table is under going provisioning and the IOPS are not guaranteed at this time.
        /// </summary>
        Provisioning,

        /// <summary>
        /// The table is ready to handle the provisioned IOPS.
        /// </summary>
        Ready,
    }
}