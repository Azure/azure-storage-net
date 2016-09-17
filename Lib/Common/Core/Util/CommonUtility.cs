//-----------------------------------------------------------------------
// <copyright file="CommonUtility.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.File;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Threading;
    using System.Xml;

#if WINDOWS_DESKTOP 
    using System.Net;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
#endif

    internal static class CommonUtility
    {
        /// <summary>
        /// Determines which location can the listing command target by looking at the
        /// continuation token.
        /// </summary>
        /// <param name="token">Continuation token</param>
        /// <returns>Location mode</returns>
        internal static CommandLocationMode GetListingLocationMode(IContinuationToken token)
        {
            if ((token != null) && token.TargetLocation.HasValue)
            {
                switch (token.TargetLocation.Value)
                {
                    case StorageLocation.Primary:
                        return CommandLocationMode.PrimaryOnly;

                    case StorageLocation.Secondary:
                        return CommandLocationMode.SecondaryOnly;

                    default:
                        CommonUtility.ArgumentOutOfRange("TargetLocation", token.TargetLocation.Value);
                        break;
                }
            }

            return CommandLocationMode.PrimaryOrSecondary;
        }

        /// <summary>
        /// Create an ExecutionState object that can be used for pre-request operations
        /// such as buffering user's data.
        /// </summary>
        /// <param name="options">Request options</param>
        /// <returns>Temporary ExecutionState object</returns>
        internal static ExecutionState<NullType> CreateTemporaryExecutionState(BlobRequestOptions options)
        {
            RESTCommand<NullType> cmdWithTimeout = new RESTCommand<NullType>(new StorageCredentials(), null /* Uri */);
            if (options != null)
            {
                options.ApplyToStorageCommand(cmdWithTimeout);
            }

            return new ExecutionState<NullType>(cmdWithTimeout, options != null ? options.RetryPolicy : null, new OperationContext());
        }

        /// <summary>
        /// Create an ExecutionState object that can be used for pre-request operations
        /// such as buffering user's data.
        /// </summary>
        /// <param name="options">Request options</param>
        /// <returns>Temporary ExecutionState object</returns>
        internal static ExecutionState<NullType> CreateTemporaryExecutionState(FileRequestOptions options)
        {
            RESTCommand<NullType> cmdWithTimeout = new RESTCommand<NullType>(new StorageCredentials(), null /* Uri */);
            if (options != null)
            {
                options.ApplyToStorageCommand(cmdWithTimeout);
            }

            return new ExecutionState<NullType>(cmdWithTimeout, options != null ? options.RetryPolicy : null, new OperationContext());
        }

        /// <summary>
        /// Returns the larger of two time spans.
        /// </summary>
        /// <param name="val1">The first of two time spans to compare.</param>
        /// <param name="val2">The second of two time spans to compare.</param>
        /// <returns>Parameter <paramref name="val1"/> or <paramref name="val2"/>, whichever is larger.</returns>
        public static TimeSpan MaxTimeSpan(TimeSpan val1, TimeSpan val2)
        {
            return val1 > val2 ? val1 : val2;
        }

        /// <summary>
        /// Gets the first header value or <c>null</c> if no header values exist.
        /// </summary>
        /// <typeparam name="T">The type of header objects contained in the enumerable.</typeparam>
        /// <param name="headerValues">An enumerable that contains header values.</param>
        /// <returns>The first header value or <c>null</c> if no header values exist.</returns>
        public static string GetFirstHeaderValue<T>(IEnumerable<T> headerValues) where T : class
        {
            if (headerValues != null)
            {
                T result = headerValues.FirstOrDefault();
                if (result != null)
                {
                    return result.ToString().TrimStart();
                }
            }

            return null;
        }

        /// <summary>
        /// Throws an exception if the string is empty or <c>null</c>.
        /// </summary>
        /// <param name="paramName">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <exception cref="ArgumentException">Thrown if value is empty.</exception>
        /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
        internal static void AssertNotNullOrEmpty(string paramName, string value)
        {
            AssertNotNull(paramName, value);

            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(SR.ArgumentEmptyError, paramName);
            }
        }

        /// <summary>
        /// Throw an exception if the value is null.
        /// </summary>
        /// <param name="paramName">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
        internal static void AssertNotNull(string paramName, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        /// <summary>
        /// Throw an exception indicating argument is out of range.
        /// </summary>
        /// <param name="paramName">The name of the parameter.</param>
        /// <param name="value">The value of the parameter.</param>
        internal static void ArgumentOutOfRange(string paramName, object value)
        {
            throw new ArgumentOutOfRangeException(paramName, string.Format(CultureInfo.InvariantCulture, SR.ArgumentOutOfRangeError, value));
        }

        /// <summary>
        /// Throw an exception if the argument is out of bounds.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="paramName">The name of the parameter.</param>
        /// <param name="val">The value of the parameter.</param>
        /// <param name="min">The minimum value for the parameter.</param>
        /// <param name="max">The maximum value for the parameter.</param>
#if WINDOWS_PHONE || !WINDOWS_DESKTOP
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
#endif
        internal static void AssertInBounds<T>(string paramName, T val, T min, T max)
            where T : IComparable
        {
            if (val.CompareTo(min) < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, string.Format(CultureInfo.InvariantCulture, SR.ArgumentTooSmallError, paramName, min));
            }

            if (val.CompareTo(max) > 0)
            {
                throw new ArgumentOutOfRangeException(paramName, string.Format(CultureInfo.InvariantCulture, SR.ArgumentTooLargeError, paramName, max));
            }
        }

        /// <summary>
        /// Throw an exception if the argument is out of bounds.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="paramName">The name of the parameter.</param>
        /// <param name="val">The value of the parameter.</param>
        /// <param name="min">The minimum value for the parameter.</param>
#if WINDOWS_PHONE || !WINDOWS_DESKTOP
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoOptimization)]
#endif
        internal static void AssertInBounds<T>(string paramName, T val, T min)
            where T : IComparable
        {
            if (val.CompareTo(min) < 0)
            {
                throw new ArgumentOutOfRangeException(paramName, string.Format(CultureInfo.InvariantCulture, SR.ArgumentTooSmallError, paramName, min));
            }
        }

        /// <summary>
        /// Combines AssertNotNullOrEmpty and AssertInBounds for convenience.
        /// </summary>
        /// <param name="paramName">The name of the parameter.</param>
        /// <param name="canBeNullOrEmpty">Turns on or off null/empty checking.</param>
        /// <param name="value">The value of the parameter.</param>
        /// <param name="maxSize">The maximum size of value.</param>
        internal static void CheckStringParameter(string paramName, bool canBeNullOrEmpty, string value, int maxSize)
        {
            if (!canBeNullOrEmpty)
            {
                AssertNotNullOrEmpty(value, paramName);
            }

            AssertInBounds(value, paramName.Length, 0, maxSize);
        }

        /// <summary>
        /// Rounds up to seconds.
        /// </summary>
        /// <param name="timeSpan">The time span.</param>
        /// <returns>The time rounded to seconds.</returns>
        internal static int RoundUpToSeconds(this TimeSpan timeSpan)
        {
            return (int)Math.Ceiling(timeSpan.TotalSeconds);
        }

        /// <summary>
        /// Appends 2 byte arrays.
        /// </summary>
        /// <param name="arr1">First array.</param>
        /// <param name="arr2">Second array.</param>
        /// <returns>The result byte array.</returns>
        internal static byte[] BinaryAppend(byte[] arr1, byte[] arr2)
        {
            int newLen = arr1.Length + arr2.Length;
            byte[] result = new byte[newLen];

            Array.Copy(arr1, result, arr1.Length);
            Array.Copy(arr2, 0, result, arr1.Length, arr2.Length);

            return result;
        }

        /// <summary>
        /// List of ports used for path style addressing.
        /// </summary>
        private static readonly int[] PathStylePorts = { 10000, 10001, 10002, 10003, 10004, 10100, 10101, 10102, 10103, 10104, 11000, 11001, 11002, 11003, 11004, 11100, 11101, 11102, 11103, 11104 };

        /// <summary>
        /// Determines if a URI requires path style addressing.
        /// </summary>
        /// <param name="uri">The URI to check.</param>
        /// <returns>Returns <c>true</c> if the Uri uses path style addressing; otherwise, <c>false</c>.</returns>
        internal static bool UsePathStyleAddressing(Uri uri)
        {
            CommonUtility.AssertNotNull("uri", uri);

            if (uri.HostNameType != UriHostNameType.Dns)
            {
                return true;
            }

            return CommonUtility.PathStylePorts.Contains(uri.Port);
        }

        /// <summary>
        /// Read the value of an element in the XML.
        /// </summary>
        /// <param name="elementName">The name of the element whose value is retrieved.</param>
        /// <param name="reader">A reader that provides access to XML data.</param>
        /// <returns>A string representation of the element's value.</returns>
        internal static string ReadElementAsString(string elementName, XmlReader reader)
        {
            string res = null;

            if (reader.IsStartElement(elementName))
            {
                if (reader.IsEmptyElement)
                {
                    reader.Skip();
                }
                else
                {
                    res = reader.ReadElementContentAsString();
                }
            }
            else
            {
                throw new XmlException(elementName);
            }

            reader.MoveToContent();

            return res;
        }

        /// <summary>
        /// Returns an enumerable collection of results that is retrieved lazily.
        /// </summary>
        /// <typeparam name="T">The type of ResultSegment like Blob, Container, Queue and Table.</typeparam>
        /// <param name="segmentGenerator">The segment generator.</param>
        /// <param name="maxResults">>A non-negative integer value that indicates the maximum number of results to be returned 
        /// in the result segment, up to the per-operation limit of 5000.</param>
        /// <returns></returns>
        internal static IEnumerable<T> LazyEnumerable<T>(Func<IContinuationToken, ResultSegment<T>> segmentGenerator, long maxResults)
        {
            ResultSegment<T> currentSeg = segmentGenerator(null);
            long count = 0;
            while (true)
            {
                foreach (T result in currentSeg.Results)
                {
                    yield return result;
                    count++;
                    if (count >= maxResults)
                    {
                        break;
                    }
                }

                if (count >= maxResults)
                {
                    break;
                }

                if (currentSeg.ContinuationToken != null)
                {
                    currentSeg = segmentGenerator(currentSeg.ContinuationToken);
                }
                else
                {
                    break;
                }
            }
        }

#if WINDOWS_DESKTOP 
        /// <summary>
        /// Applies the request optimizations such as disabling buffering and 100 continue.
        /// </summary>
        /// <param name="request">The request to be modified.</param>
        /// <param name="length">The length of the content, -1 if the content length is not settable.</param>
        internal static void ApplyRequestOptimizations(HttpWebRequest request, long length)
        {
            if (length >= Constants.DefaultBufferSize)
            {
                request.AllowWriteStreamBuffering = false;
            }

            // Set the length of the stream if the value is known
            if (length >= 0)
            {
                request.ContentLength = length;
            }

#if !(WINDOWS_PHONE && WINDOWS_DESKTOP)
            // Disable the Expect 100-Continue
            request.ServicePoint.Expect100Continue = false;
#endif
        }
#endif

        // TODO: When we move to .NET 4.5, we may be able to get rid of this method, or at least reduce our reliance upon it.
        // The ideal solution is to use async either everywhere or nowhere throughout a call to the Storage library, but this may
        // not be possible (KeyVault only exposes async APIs, and doesn't use ConfigureAwait(false), for example).
        // Blog post discussing this is here: http://blogs.msdn.com/b/pfxteam/archive/2012/04/13/10293638.aspx
        internal static void RunWithoutSynchronizationContext(Action actionToRun)
        {
            SynchronizationContext oldContext = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);
                actionToRun();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }
        }

        internal static T RunWithoutSynchronizationContext<T>(Func<T> actionToRun)
        {
            SynchronizationContext oldContext = SynchronizationContext.Current;
            try
            {
                SynchronizationContext.SetSynchronizationContext(null);
                return actionToRun();
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(oldContext);
            }
        }
    }
}
