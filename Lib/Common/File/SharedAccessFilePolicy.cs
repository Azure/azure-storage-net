//-----------------------------------------------------------------------
// <copyright file="SharedAccessFilePolicy.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System;
    using System.Text;

    /// <summary>
    /// Represents a shared access policy, which specifies the start time, expiry time, 
    /// and permissions for a shared access signature.
    /// </summary>
    public sealed class SharedAccessFilePolicy
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SharedAccessFilePolicy"/> class.
        /// </summary>
        public SharedAccessFilePolicy()
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
        /// <value>A <see cref="SharedAccessFilePermissions"/> object.</value>
        public SharedAccessFilePermissions Permissions { get; set; }

        /// <summary>
        /// Converts the permissions specified for the shared access policy to a string.
        /// </summary>
        /// <param name="permissions">A <see cref="SharedAccessFilePermissions"/> object.</param>
        /// <returns>The shared access permissions, in string format.</returns>
        public static string PermissionsToString(SharedAccessFilePermissions permissions) 
        {
            // The service supports a fixed order => rcwdl
            StringBuilder builder = new StringBuilder();

            if ((permissions & SharedAccessFilePermissions.Read) == SharedAccessFilePermissions.Read)
            {
                builder.Append("r");
            }

            if ((permissions & SharedAccessFilePermissions.Create) == SharedAccessFilePermissions.Create)
            {
                builder.Append("c");
            }

            if ((permissions & SharedAccessFilePermissions.Write) == SharedAccessFilePermissions.Write)
            {
                builder.Append("w");
            }

            if ((permissions & SharedAccessFilePermissions.Delete) == SharedAccessFilePermissions.Delete)
            {
                builder.Append("d");
            }

            if ((permissions & SharedAccessFilePermissions.List) == SharedAccessFilePermissions.List)
            {
                builder.Append("l");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Constructs a <see cref="SharedAccessFilePermissions"/> object from a permissions string.
        /// </summary>
        /// <param name="input">The shared access permissions, in string format.</param>
        /// <returns>A <see cref="SharedAccessFilePermissions"/> object.</returns>
        public static SharedAccessFilePermissions PermissionsFromString(string input) 
        {
            CommonUtility.AssertNotNull("input", input);

            SharedAccessFilePermissions permissions = 0;

            foreach (char c in input)
            {
                switch (c)
                {
                    case 'r':
                        permissions |= SharedAccessFilePermissions.Read;
                        break;

                    case 'w':
                        permissions |= SharedAccessFilePermissions.Write;
                        break;

                    case 'd':
                        permissions |= SharedAccessFilePermissions.Delete;
                        break;

                    case 'l':
                        permissions |= SharedAccessFilePermissions.List;
                        break;

                    case 'c':
                        permissions |= SharedAccessFilePermissions.Create;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException("input");
                }
            }

            // In case we ever change none to be something other than 0
            if (permissions == 0)
            {
                permissions |= SharedAccessFilePermissions.None;
            }

            return permissions;
        }
    }
}
