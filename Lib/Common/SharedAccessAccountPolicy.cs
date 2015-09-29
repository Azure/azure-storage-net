// -----------------------------------------------------------------------------------------
// <copyright file="SharedAccessAccountPolicy.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared;
    using System;
    using System.Net;
    using System.Text;

    /// <summary>
    /// Represents a shared access policy for a account, which specifies the start time, expiry time, 
    /// permissions, signed service, signed resource type, signed protocol, and signed IP addresses for a shared access signature.
    /// </summary>
    public sealed class SharedAccessAccountPolicy
    {
        /// <summary>
        /// Initializes a new instance of the SharedAccessAccountPolicy class.
        /// </summary>
        public SharedAccessAccountPolicy()
        {
        }

        /// <summary>
        /// Gets or sets the start time for a shared access signature associated with this shared access policy.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> specifying the shared access start time.</value>
        public DateTimeOffset? SharedAccessStartTime { get; set; }

        /// <summary>
        /// Gets or sets the expiry time for a shared access signature associated with this shared access policy.
        /// </summary>
        /// <value>A <see cref="DateTimeOffset"/> specifying the shared access expiry time.</value>
        public DateTimeOffset? SharedAccessExpiryTime { get; set; }

        /// <summary>
        /// Gets or sets the permissions for a shared access signature associated with this shared access policy.
        /// </summary>
        /// <value>A <see cref="SharedAccessAccountPermissions"/> object.</value>
        public SharedAccessAccountPermissions Permissions { get; set; }

        /// <summary>
        /// Gets or sets the services (blob, file, queue, table) for a shared access signature associated with this shared access policy.
        /// </summary>
        public SharedAccessAccountServices Services { get; set; }

        /// <summary>
        /// Gets or sets the resource type for a shared access signature associated with this shared access policy.
        /// </summary>
        public SharedAccessAccountResourceTypes ResourceTypes { get; set; }

        /// <summary>
        /// Gets or sets the allowed protocols for a shared access signature associated with this shared access policy.
        /// </summary>
        public SharedAccessProtocol? Protocols { get; set; }

        /// <summary>
        /// Gets or sets the allowed IP address or IP address range for a shared access signature associated with this shared access policy.
        /// </summary>
        public IPAddressOrRange IPAddressOrRange { get; set; }

        /// <summary>
        /// Converts the permissions specified for the shared access policy to a string.
        /// </summary>
        /// <param name="permissions">A <see cref="SharedAccessAccountPermissions"/> object.</param>
        /// <returns>The shared access permissions in string format.</returns>
        public static string PermissionsToString(SharedAccessAccountPermissions permissions)
        {
            StringBuilder builder = new StringBuilder();

            if ((permissions & SharedAccessAccountPermissions.Read) == SharedAccessAccountPermissions.Read)
            {
                builder.Append("r");
            }

            if ((permissions & SharedAccessAccountPermissions.Add) == SharedAccessAccountPermissions.Add)
            {
                builder.Append("a");
            }

            if ((permissions & SharedAccessAccountPermissions.Create) == SharedAccessAccountPermissions.Create)
            {
                builder.Append("c");
            }

            if ((permissions & SharedAccessAccountPermissions.Update) == SharedAccessAccountPermissions.Update)
            {
                builder.Append("u");
            }

            if ((permissions & SharedAccessAccountPermissions.ProcessMessages) == SharedAccessAccountPermissions.ProcessMessages)
            {
                builder.Append("p");
            }

            if ((permissions & SharedAccessAccountPermissions.Write) == SharedAccessAccountPermissions.Write)
            {
                builder.Append("w");
            }

            if ((permissions & SharedAccessAccountPermissions.Delete) == SharedAccessAccountPermissions.Delete)
            {
                builder.Append("d");
            }

            if ((permissions & SharedAccessAccountPermissions.List) == SharedAccessAccountPermissions.List)
            {
                builder.Append("l");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Converts the services specified for the shared access policy to a string.
        /// </summary>
        /// <param name="services">A <see cref="SharedAccessAccountServices"/> object.</param>
        /// <returns>The shared access services in string format.</returns>
        public static string ServicesToString(SharedAccessAccountServices services)
        {
            StringBuilder builder = new StringBuilder();

            if ((services & SharedAccessAccountServices.Blob) == SharedAccessAccountServices.Blob)
            {
                builder.Append("b");
            }

            if ((services & SharedAccessAccountServices.File) == SharedAccessAccountServices.File)
            {
                builder.Append("f");
            }

            if ((services & SharedAccessAccountServices.Queue) == SharedAccessAccountServices.Queue)
            {
                builder.Append("q");
            }

            if ((services & SharedAccessAccountServices.Table) == SharedAccessAccountServices.Table)
            {
                builder.Append("t");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Converts the ResourceTypes specified for the shared access policy to a string.
        /// </summary>
        /// <param name="resourceTypes">A <see cref="SharedAccessAccountResourceTypes"/> object.</param>
        /// <returns>The shared access resource types in string format.</returns>
        public static string ResourceTypesToString(SharedAccessAccountResourceTypes resourceTypes)
        {
            StringBuilder builder = new StringBuilder();

            if ((resourceTypes & SharedAccessAccountResourceTypes.Service) == SharedAccessAccountResourceTypes.Service)
            {
                builder.Append("s");
            }

            if ((resourceTypes & SharedAccessAccountResourceTypes.Container) == SharedAccessAccountResourceTypes.Container)
            {
                builder.Append("c");
            }

            if ((resourceTypes & SharedAccessAccountResourceTypes.Object) == SharedAccessAccountResourceTypes.Object)
            {
                builder.Append("o");
            }

            return builder.ToString();
        }
    }
}


