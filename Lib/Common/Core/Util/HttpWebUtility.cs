//-----------------------------------------------------------------------
// <copyright file="HttpWebUtility.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core.Util
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net.Http.Headers;

#if WINDOWS_DESKTOP
    using System.Net;
    using System.Net.Http;
    using System.Collections.ObjectModel;
#endif

    /// <summary>
    /// Provides helper functions for http request/response processing. 
    /// </summary>
    internal static class HttpWebUtility
    {
        /// <summary>
        /// Parse the http query string.
        /// </summary>
        /// <param name="query">Http query string.</param>
        /// <returns></returns>
        public static IDictionary<string, string> ParseQueryString(string query)
        {
            Dictionary<string, ICollection<string>> retVal = new Dictionary<string, ICollection<string>>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(query))
            {
                return retVal.ToDictionary(kvp => kvp.Key, kvp => string.Join(",", kvp.Value.OrderBy(_ => _)));
            }

            if (query.StartsWith("?", StringComparison.Ordinal))
            {
                if (query.Length == 1)
                {
                    return retVal.ToDictionary(kvp => kvp.Key, kvp => string.Join(",", kvp.Value.OrderBy(_ => _)));
                }

                query = query.Substring(1);
            }

            string[] valuePairs = query.Split('&');
            foreach (string pair in valuePairs)
            {
                string key;
                string value;

                int equalDex = pair.IndexOf("=", StringComparison.Ordinal);
                if (equalDex < 0)
                {
                    key = string.Empty;
                    value = Uri.UnescapeDataString(pair);
                }
                else
                {
                    key = Uri.UnescapeDataString(pair.Substring(0, equalDex));
                    value = Uri.UnescapeDataString(pair.Substring(equalDex + 1));
                }

                if (retVal.TryGetValue(key, out ICollection<string> existingValue))
                {
                    existingValue.Add(value);
                }
                else
                {
                    retVal[key] = new List<string> { value };
                }
            }

            return retVal.ToDictionary(kvp => kvp.Key, kvp => string.Join(",", kvp.Value.OrderBy(_ => _)));
        }

        /// <summary>
        /// Converts the DateTimeOffset object to an Http string of form: Mon, 28 Jan 2008 12:11:37 GMT.
        /// </summary>
        /// <param name="dateTime">The DateTimeOffset object to convert to an Http string.</param>
        /// <returns>String of form: Mon, 28 Jan 2008 12:11:37 GMT.</returns>
        public static string ConvertDateTimeToHttpString(DateTimeOffset dateTime)
        {
            // 'R' means rfc1123 date which is what the storage services use for all dates...
            // It will be in the following format:
            // Mon, 28 Jan 2008 12:11:37 GMT
            return dateTime.UtcDateTime.ToString("R", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Combine all the header values in the IEnumerable to a single comma separated string.
        /// </summary>
        /// <param name="headerValues">An IEnumerable<string> object representing the header values.</string></param>
        /// <returns>A comma separated string of header values.</returns>
        public static string CombineHttpHeaderValues(IEnumerable<string> headerValues)
        {
            if (headerValues == null)
            {
                return null;
            }

            return (headerValues.Count() == 0) ?
                null :
                string.Join(",", headerValues);
        }

        public static string GetHeaderValues(string headerName, HttpHeaders headers)
        {
            IEnumerable<string> headerValues = null;

            if (headers.TryGetValues(headerName, out headerValues))
            {
                return CombineHttpHeaderValues(headerValues);
            }

            return null;
        }
    }
}
