//-----------------------------------------------------------------------
// <copyright file="AsyncExtensions.Common.cs" company="Microsoft">
//    Copyright 2018 Microsoft Corporation
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

namespace Microsoft.Azure.Storage.Core.Util
{
    using Shared.Protocol;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;

    internal static partial class XMLReaderExtensions
    {
        public static XmlReader CreateAsAsync(Stream stream)
        {
            return XmlReader.Create(
                stream,
                new XmlReaderSettings
                {
                    IgnoreWhitespace = true,
                    Async = true
                }
                );
        }

        // The async XMLReader methods don't offer CancellationToken support.
        // Rather than use .WithCancellation(token) on all of them, we can just call
        // token.ThrowIfCancellationRequested periodically in the parsers

        // Copied from https://msdn.microsoft.com/en-us/library/system.xml.xmlreader(v=vs.110).aspx
        // Note that this changes the exception types.
        public static async Task ReadStartElementAsync(this XmlReader reader, string localname, string ns)
        {
            if (await reader.MoveToContentAsync().ConfigureAwait(false) != XmlNodeType.Element)
            {
                throw new InvalidOperationException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
            }
            if ((reader.LocalName == localname) && (reader.NamespaceURI == ns))
            {
                await reader.ReadAsync().ConfigureAwait(false);
            }
            else
            {
                throw new InvalidOperationException("localName or namespace doesn’t match");
            }
        }

        public static async Task ReadStartElementAsync(this XmlReader reader)
        {
            if (await reader.MoveToContentAsync().ConfigureAwait(false) != XmlNodeType.Element)
            {
                throw new InvalidOperationException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
            }
            await reader.ReadAsync().ConfigureAwait(false);
        }

        // Copied from https://msdn.microsoft.com/en-us/library/system.xml.xmlreader(v=vs.110).aspx
        // Note that this changes the exception types.
        public static async Task ReadEndElementAsync(this XmlReader reader)
        {
            if (await reader.MoveToContentAsync().ConfigureAwait(false) != XmlNodeType.EndElement)
            {
                throw new InvalidOperationException();
            }
            await reader.ReadAsync().ConfigureAwait(false);
        }

        // Copied and modified from https://referencesource.microsoft.com/#System.Xml/System/Xml/Core/XmlReader.cs
        public static async Task<string> ReadElementContentAsStringAsync(this XmlReader reader, string localName, string namespaceURI)
        {
            reader.CheckElement(localName, namespaceURI);
            return await reader.ReadElementContentAsStringAsync().ConfigureAwait(false);
        }

        // Copied and modified from https://referencesource.microsoft.com/#System.Xml/System/Xml/Core/XmlReader.cs
        public static async Task<Boolean> ReadElementContentAsBooleanAsync(this XmlReader reader)
        {
            return XmlConvert.ToBoolean(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
        }

        // Copied and modified from https://referencesource.microsoft.com/#System.Xml/System/Xml/Core/XmlReader.cs
        public static async Task<Boolean> ReadElementContentAsBooleanAsync(this XmlReader reader, string localName, string namespaceURI)
        {
            reader.CheckElement(localName, namespaceURI);
            return XmlConvert.ToBoolean(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
        }

        // Copied and modified from https://referencesource.microsoft.com/#System.Xml/System/Xml/Core/XmlReader.cs
        public static async Task<Int32> ReadElementContentAsInt32Async(this XmlReader reader)
        {
            return XmlConvert.ToInt32(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
        }

        // Copied and modified from https://referencesource.microsoft.com/#System.Xml/System/Xml/Core/XmlReader.cs
        public static async Task<Int32> ReadElementContentAsInt32Async(this XmlReader reader, string localName, string namespaceURI)
        {
            reader.CheckElement(localName, namespaceURI);
            return XmlConvert.ToInt32(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
        }

        // Copied and modified from https://referencesource.microsoft.com/#System.Xml/System/Xml/Core/XmlReader.cs
        public static async Task<Int64> ReadElementContentAsInt64Async(this XmlReader reader)
        {
            return XmlConvert.ToInt64(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
        }

        // Copied and modified from https://referencesource.microsoft.com/#System.Xml/System/Xml/Core/XmlReader.cs
        public static async Task<Int64> ReadElementContentAsInt64Async(this XmlReader reader, string localName, string namespaceURI)
        {
            reader.CheckElement(localName, namespaceURI);
            return XmlConvert.ToInt64(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
        }

        // Copied and modified from https://referencesource.microsoft.com/#System.Xml/System/Xml/Core/XmlReader.cs
        public static async Task<DateTimeOffset> ReadElementContentAsDateTimeOffsetAsync(this XmlReader reader)
        {
            return (await reader.ReadElementContentAsStringAsync().ConfigureAwait(false)).ToUTCTime();
        }

        // Copied and modified from https://referencesource.microsoft.com/#System.Xml/System/Xml/Core/XmlReader.cs
        public static async Task<DateTimeOffset> ReadElementContentAsDateTimeOffsetAsync(this XmlReader reader, string localName, string namespaceURI)
        {
            reader.CheckElement(localName, namespaceURI);
            return XmlConvert.ToDateTimeOffset(await reader.ReadElementContentAsStringAsync().ConfigureAwait(false));
        }

        // Copied and modified from https://referencesource.microsoft.com/#System.Xml/System/Xml/Core/XmlReader.cs
        // Note that this changes the exception types.
        private static void CheckElement(this XmlReader reader, string localName, string namespaceURI)
        {
            if (localName == null || localName.Length == 0)
            {
                throw new InvalidOperationException("localName is null or empty");
            }
            if (namespaceURI == null)
            {
                throw new ArgumentNullException("namespaceURI");
            }
            if (reader.NodeType != XmlNodeType.Element)
            {
                throw new InvalidOperationException(reader.NodeType.ToString() + " is an invalid XmlNodeType");
            }
            if (reader.LocalName != localName || reader.NamespaceURI != namespaceURI)
            {
                throw new InvalidOperationException("localName or namespace doesn’t match");
            }
        }

        public static async Task<bool> IsStartElementAsync(this XmlReader reader)
        {
            return (await reader.MoveToContentAsync().ConfigureAwait(false)) == XmlNodeType.Element;
        }

        public static async Task<bool> IsStartElementAsync(this XmlReader reader, string name)
        {
            return (await reader.MoveToContentAsync().ConfigureAwait(false) == XmlNodeType.Element) &&
                   (reader.Name == name);
        }

        public static async Task<bool> ReadToFollowingAsync(this XmlReader reader, string localName, string namespaceURI)
        {
            if (localName == null || localName.Length == 0)
            {
                throw new ArgumentException("localName is empty or null");
            }
            if (namespaceURI == null)
            {
                throw new ArgumentNullException("namespaceURI");
            }

            // atomize local name and namespace
            localName = reader.NameTable.Add(localName);
            namespaceURI = reader.NameTable.Add(namespaceURI);

            // find element with that name
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (reader.NodeType == XmlNodeType.Element && ((object)localName == (object)reader.LocalName) && ((object)namespaceURI == (object)reader.NamespaceURI))
                {
                    return true;
                }
            }
            return false;
        }

        public static async Task<bool> ReadToFollowingAsync(this XmlReader reader, string localName)
        {
            if (localName == null || localName.Length == 0)
            {
                throw new ArgumentException("localName is empty or null");
            }

            // atomize local name and namespace
            localName = reader.NameTable.Add(localName);

            // find element with that name
            while (await reader.ReadAsync().ConfigureAwait(false))
            {
                if (reader.NodeType == XmlNodeType.Element && ((object)localName == (object)reader.LocalName))
                {
                    return true;
                }
            }
            return false;
        }
    }

    // From https://blogs.msdn.microsoft.com/pfxteam/2012/02/11/building-async-coordination-primitives-part-1-asyncmanualresetevent/
    public class AsyncManualResetEvent
    {
        private volatile TaskCompletionSource<bool> m_tcs = new TaskCompletionSource<bool>();

        public AsyncManualResetEvent(bool initialStateSignaled)
        {
            Task.Run(() => m_tcs.TrySetResult(initialStateSignaled));
        }

        public Task WaitAsync() { return m_tcs.Task; }

        public async Task Set()
        {
            TaskCompletionSource<bool> tcs = m_tcs;
            await Task.Factory.StartNew(s => ((TaskCompletionSource<bool>)s).TrySetResult(true),
                tcs, CancellationToken.None, TaskCreationOptions.PreferFairness, TaskScheduler.Default).ConfigureAwait(false);
            await tcs.Task.ConfigureAwait(false);
        }

        public void Reset()
        {
            while (true)
            {
                TaskCompletionSource<bool> tcs = m_tcs;
                if (!tcs.Task.IsCompleted ||
                    Interlocked.CompareExchange(ref m_tcs, new TaskCompletionSource<bool>(), tcs) == tcs)
                    return;
            }
        }
    }
}
