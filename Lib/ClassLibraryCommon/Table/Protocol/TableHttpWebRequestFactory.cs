﻿//-----------------------------------------------------------------------
// <copyright file="TableWebRequestFactory.cs" company="Microsoft">
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

namespace Microsoft.Azure.Storage.Table.Protocol
{
    using Microsoft.Azure.Storage.Core;
    using Microsoft.Azure.Storage.Core.Util;
    using Microsoft.Azure.Storage.Shared.Protocol;
    using System;
    using System.IO;
    using System.Net;

    /// <summary>
    /// A factory class for constructing a web request to manage tables in the Table service.
    /// </summary>
    public static class TableHttpWebRequestFactory
    {
        /// <summary>
        /// Creates a web request to get the properties of the Table service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Table service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, OperationContext operationContext)
        {
            return TableHttpWebRequestFactory.GetServiceProperties(uri, builder, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Creates a web request to get the properties of the Table service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Table service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        internal static HttpWebRequest GetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.GetServiceProperties(uri, builder, timeout, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Creates a web request to set the properties of the Table service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Table service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, OperationContext operationContext)
        {
            return TableHttpWebRequestFactory.SetServiceProperties(uri, builder, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Creates a web request to set the properties of the Table service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Table service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        internal static HttpWebRequest SetServiceProperties(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.SetServiceProperties(uri, builder, timeout, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Creates a web request to get the stats of the Table service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Table service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetServiceStats(Uri uri, UriQueryBuilder builder, int? timeout, OperationContext operationContext)
        {
            return TableHttpWebRequestFactory.GetServiceStats(uri, builder, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Creates a web request to get the stats of the Table service.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the Table service endpoint.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">The server timeout interval, in seconds.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        internal static HttpWebRequest GetServiceStats(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.GetServiceStats(uri, builder, timeout, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Writes Table service properties to a stream, formatted in XML.
        /// </summary>
        /// <param name="properties">A <see cref="ServiceProperties"/> object containing the service properties to format and write to the stream.</param>
        /// <param name="outputStream">The <see cref="System.IO.Stream"/> object to which the formatted properties are to be written.</param>
        public static void WriteServiceProperties(ServiceProperties properties, Stream outputStream)
        {
            CommonUtility.AssertNotNull("properties", properties);

            properties.WriteServiceProperties(outputStream);
        }

        /// <summary>
        /// Constructs a web request to return the ACL for a table.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI for the table.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetAcl(Uri uri, UriQueryBuilder builder, int? timeout, OperationContext operationContext)
        {
            return TableHttpWebRequestFactory.GetAcl(uri, builder, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to return the ACL for a table.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI for the table.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest GetAcl(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.GetAcl(uri, builder, timeout, useVersionHeader, operationContext);
        }

        /// <summary>
        /// Constructs a web request to set the ACL for a table.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI for the table.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetAcl(Uri uri, UriQueryBuilder builder, int? timeout, OperationContext operationContext)
        {
            return TableHttpWebRequestFactory.SetAcl(uri, builder, timeout, true /* useVersionHeader */, operationContext);
        }

        /// <summary>
        /// Constructs a web request to set the ACL for a table.
        /// </summary>
        /// <param name="uri">A <see cref="System.Uri"/> specifying the absolute URI for the table.</param>
        /// <param name="builder">A <see cref="UriQueryBuilder"/> object specifying additional parameters to add to the URI query string.</param>
        /// <param name="timeout">An integer specifying the server timeout interval.</param>
        /// <param name="useVersionHeader">A boolean value indicating whether to set the <i>x-ms-version</i> HTTP header.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="System.Net.HttpWebRequest"/> object.</returns>
        public static HttpWebRequest SetAcl(Uri uri, UriQueryBuilder builder, int? timeout, bool useVersionHeader, OperationContext operationContext)
        {
            return HttpWebRequestFactory.SetAcl(uri, builder, timeout, useVersionHeader, operationContext);
        }       
    }
}