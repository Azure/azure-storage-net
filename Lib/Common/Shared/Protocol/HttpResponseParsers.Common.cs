﻿// -----------------------------------------------------------------------------------------
// <copyright file="HttpResponseParsers.Common.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Shared.Protocol
{
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Executor;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;

    internal static partial class HttpResponseParsers
    {
        /// <summary>
        /// Converts a string to UTC time.
        /// </summary>
        /// <param name="str">The string to convert.</param>
        /// <returns>A UTC representation of the string.</returns>
        internal static DateTime ToUTCTime(this string str)
        {
            return DateTime.Parse(
                str,
                System.Globalization.DateTimeFormatInfo.InvariantInfo,
                System.Globalization.DateTimeStyles.AdjustToUniversal);
        }

        internal static T ProcessExpectedStatusCodeNoException<T>(HttpStatusCode expectedStatusCode, HttpStatusCode actualStatusCode, T retVal, StorageCommandBase<T> cmd, Exception ex)
        {
            if (ex != null)
            {
                throw ex;
            }

            if (actualStatusCode != expectedStatusCode)
            {
                throw new StorageException(cmd.CurrentResult, string.Format(CultureInfo.InvariantCulture, SR.UnexpectedResponseCode, expectedStatusCode, actualStatusCode), null);
            }

            return retVal;
        }

        internal static T ProcessExpectedStatusCodeNoException<T>(HttpStatusCode[] expectedStatusCodes, HttpStatusCode actualStatusCode, T retVal, StorageCommandBase<T> cmd, Exception ex)
        {
            if (ex != null)
            {
                throw ex;
            }

            if (!expectedStatusCodes.Contains(actualStatusCode))
            {
                string expectedStatusCodeString = string.Join(",", expectedStatusCodes);
                throw new StorageException(cmd.CurrentResult, string.Format(CultureInfo.InvariantCulture, SR.UnexpectedResponseCode, expectedStatusCodeString, actualStatusCode.ToString()), null);
            }

            return retVal;
        }

        internal static void ValidateResponseStreamChecksumAndLength<T>(long? length, string md5, string crc64, StorageCommandBase<T> cmd)
        {
            if (cmd.StreamCopyState == null)
            {
                throw new StorageException(
                    cmd.CurrentResult,
                    SR.ContentChecksumNotCalculated,
                    null)
                {
                    IsRetryable = false
                };
            }

            if (length.HasValue && (cmd.StreamCopyState.Length != length.Value))
            {
                throw new StorageException(
                    cmd.CurrentResult,
                    string.Format(CultureInfo.InvariantCulture, SR.IncorrectNumberOfBytes, length, cmd.StreamCopyState.Length),
                    null)
                    {
                        IsRetryable = false
                    };
            }

            if ((md5 != null) && (cmd.StreamCopyState.Md5 != null) && (cmd.StreamCopyState.Md5 != md5))
            {
                throw new StorageException(
                    cmd.CurrentResult,
                    SR.MD5MismatchError,
                    null)
                    {
                        IsRetryable = false
                    };
            }

            if ((crc64 != null) && (cmd.StreamCopyState.Crc64 != null) && (cmd.StreamCopyState.Crc64 != crc64))
            {
                throw new StorageException(
                    cmd.CurrentResult,
                    SR.CRC64MismatchError,
                    null)
                {
                    IsRetryable = false
                };
            }
        }

        /// <summary>
        /// Reads account properties from an HttpResponseMessage object.
        /// </summary>
        /// <param name="response">The response from which to read the account properties.</param>
        /// <returns>The account properties stored in the headers.</returns>
        internal static AccountProperties ReadAccountProperties(HttpResponseMessage response)
        {
            return AccountProperties.FromHttpResponseHeaders(response.Headers);
        }

        /// <summary>
        /// Reads service properties from a stream.
        /// </summary>
        /// <param name="inputStream">The stream from which to read the service properties.</param>
        /// <returns>The service properties stored in the stream.</returns>
        internal static Task<ServiceProperties> ReadServicePropertiesAsync(Stream inputStream, CancellationToken token)
        {
            return Task.Run(
                () =>
                {
                    using (XmlReader reader = XmlReader.Create(inputStream))
                    {
                        XDocument servicePropertyDocument = XDocument.Load(reader);

                        return ServiceProperties.FromServiceXml(servicePropertyDocument);
                    }
                },
                token
                );
        }

        /// <summary>
        /// Reads service stats from a stream.
        /// </summary>
        /// <param name="inputStream">The stream from which to read the service stats.</param>
        /// <returns>The service stats stored in the stream.</returns>
        internal static Task<ServiceStats> ReadServiceStatsAsync(Stream inputStream, CancellationToken token)
        {
            return Task.Run(
                () =>
                {
                    using (XmlReader reader = XmlReader.Create(inputStream))
                    {
                        XDocument serviceStatsDocument = XDocument.Load(reader);

                        return ServiceStats.FromServiceXml(serviceStatsDocument);
                    }
                },
                token
                );
        }
    }
}
