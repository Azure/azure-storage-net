// -----------------------------------------------------------------------------------------
// <copyright file="SharedKeyLiteTableCanonicalizer.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core.Auth
{
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using System.Net.Http;

    internal sealed class SharedKeyLiteTableCanonicalizer : ICanonicalizer
    {
        private const string SharedKeyLiteAuthorizationScheme = "SharedKeyLite";
        private const int ExpectedCanonicalizedStringLength = 150;

        private static SharedKeyLiteTableCanonicalizer instance = new SharedKeyLiteTableCanonicalizer();

        public static SharedKeyLiteTableCanonicalizer Instance
        {
            get
            {
                return SharedKeyLiteTableCanonicalizer.instance;
            }
        }

        private SharedKeyLiteTableCanonicalizer()
        {
        }

        public string AuthorizationScheme
        {
            get
            {
                return SharedKeyLiteAuthorizationScheme;
            }
        }

        public string CanonicalizeHttpRequest(HttpRequestMessage request, string accountName)
        {
            // Add the Date HTTP header (or the x-ms-date header if it is being used)
            string dateHeaderValue = AuthenticationUtility.GetPreferredDateHeaderValue(request);
            CanonicalizedString canonicalizedString = new CanonicalizedString(dateHeaderValue, ExpectedCanonicalizedStringLength);

            // Add the canonicalized URI element
            string resourceString = AuthenticationUtility.GetCanonicalizedResourceString(request.RequestUri, accountName, true);
            canonicalizedString.AppendCanonicalizedElement(resourceString);

            return canonicalizedString.ToString();
        }
    }
}
