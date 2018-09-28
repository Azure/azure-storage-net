//-----------------------------------------------------------------------
// <copyright file="FileRequest.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.File.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// Provides a set of helper methods for constructing a request against the File service.
    /// </summary>
#if WINDOWS_RT
    internal
#else
    public
#endif
        static class FileRequest
    {
        /// <summary>
        /// Writes a collection of shared access policies to the specified stream in XML format.
        /// </summary>
        /// <param name="sharedAccessPolicies">A collection of shared access policies.</param>
        /// <param name="outputStream">An output stream.</param>
        public static void WriteSharedAccessIdentifiers(SharedAccessFilePolicies sharedAccessPolicies, Stream outputStream)
        {
            Request.WriteSharedAccessIdentifiers(
                sharedAccessPolicies,
                outputStream,
                (policy, writer) =>
                {
                    writer.WriteElementString(
                        Constants.Start,
                        SharedAccessSignatureHelper.GetDateTimeOrEmpty(policy.SharedAccessStartTime));
                    writer.WriteElementString(
                        Constants.Expiry,
                        SharedAccessSignatureHelper.GetDateTimeOrEmpty(policy.SharedAccessExpiryTime));
                    writer.WriteElementString(
                        Constants.Permission,
                        SharedAccessFilePolicy.PermissionsToString(policy.Permissions));
                });
        }
    }
}
