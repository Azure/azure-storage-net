// -----------------------------------------------------------------------------------------
// <copyright file="QueueHttpResponseParsers.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Queue.Protocol
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System.Collections.Generic;
    using System.Net.Http;
    using System;

    public static partial class QueueHttpResponseParsers
    {
        /// <summary>
        /// Gets the approximate message count for the queue.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The approximate count for the queue.</returns>
        public static string GetApproximateMessageCount(HttpResponseMessage response)
        {
            CommonUtility.AssertNotNull("response", response);
            return response.Headers.GetHeaderSingleValueOrDefault(Constants.HeaderConstants.ApproximateMessagesCount);
        }

        /// <summary>
        /// Gets the user-defined metadata.
        /// </summary>
        /// <param name="response">The response from server.</param>
        /// <returns>A <see cref="IDictionary"/> of the metadata.</returns>
        public static IDictionary<string, string> GetMetadata(HttpResponseMessage response)
        {
            return HttpResponseParsers.GetMetadata(response);
        }

        /// <summary>
        /// Extracts the pop receipt from a web response header.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The pop receipt stored in the header of the response.</returns>
        public static string GetPopReceipt(HttpResponseMessage response)
        {
            CommonUtility.AssertNotNull("response", response);

            return HttpResponseParsers.GetHeader(response, Constants.HeaderConstants.PopReceipt);
        }

        /// <summary>
        /// Extracts the next visibility time from a response message header.
        /// </summary>
        /// <param name="response">The web response.</param>
        /// <returns>The time of next visibility stored in the header of the response.</returns>
        public static DateTime GetNextVisibleTime(HttpResponseMessage response)
        {
            CommonUtility.AssertNotNull("response", response);

            return DateTime.Parse(
                HttpResponseParsers.GetHeader(response, Constants.HeaderConstants.NextVisibleTime),
                System.Globalization.DateTimeFormatInfo.InvariantInfo,
                System.Globalization.DateTimeStyles.AdjustToUniversal);
        }
    }
}
