// -----------------------------------------------------------------------------------------
// <copyright file="TestBase.cs" company="Microsoft">
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

using Microsoft.WindowsAzure.Storage.Auth;
using System;
using System.IO;
using System.Xml.Linq;
using Windows.ApplicationModel;
using Windows.Data.Xml.Dom;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Microsoft.WindowsAzure.Storage
{
    public partial class TestBase
    {
        static TestBase()
        {
            try
            {
                StorageFile xmlFile = Package.Current.InstalledLocation.GetFileAsync(TestConfigurations.DefaultTestConfigFilePath).AsTask().Result;
                XmlDocument xmlDoc = XmlDocument.LoadFromFileAsync(xmlFile).AsTask().Result;

                XDocument doc = XDocument.Parse(xmlDoc.GetXml());
                TestConfigurations configurations = TestConfigurations.ReadFromXml(doc);

                TestBase.Initialize(configurations);
            }
            catch (System.IO.FileNotFoundException)
            {
                throw new System.IO.FileNotFoundException("To run tests you need to supply a TestConfigurations.xml file with credentials in the Test/Common folder. Use TestConfigurationsTemplate.xml as a template.");
            }
        }
    }

#if WINDOWS_RT
    public static class MicrosoftStreamExtensions
    {
        public static IRandomAccessStreamWithContentType AsRandomAccessStream(this Stream stream)
        {
            return new RandomStream(stream);
        }

    }

    class RandomStream : IRandomAccessStreamWithContentType
    {
        Stream internstream;

        public RandomStream(Stream underlyingstream)
        {
            internstream = underlyingstream;
        }

        public IInputStream GetInputStreamAt(ulong position)
        {
            internstream.Position = (long)position;
            return internstream.AsInputStream();
        }

        public IOutputStream GetOutputStreamAt(ulong position)
        {
            internstream.Position = (long)position;
            return internstream.AsOutputStream();
        }

        public ulong Size
        {
            get
            {
                return (ulong)internstream.Length;
            }
            set
            {
                internstream.SetLength((long)value);
            }
        }

        public bool CanRead
        {
            get { return this.internstream.CanRead; }
        }

        public bool CanWrite
        {
            get { return this.internstream.CanWrite; }
        }

        public IRandomAccessStream CloneStream()
        {
            throw new NotSupportedException();
        }

        public ulong Position
        {
            get { return (ulong)this.internstream.Position; }
        }

        public void Seek(ulong position)
        {
            this.internstream.Seek((long)position, SeekOrigin.Begin);
        }

        public void Dispose()
        {
            this.internstream.Dispose();
        }

        public Windows.Foundation.IAsyncOperationWithProgress<IBuffer, uint> ReadAsync(IBuffer buffer, uint count, InputStreamOptions options)
        {
            return this.GetInputStreamAt(this.Position).ReadAsync(buffer, count, options);
        }

        public Windows.Foundation.IAsyncOperation<bool> FlushAsync()
        {
            return this.GetOutputStreamAt(this.Position).FlushAsync();
        }

        public Windows.Foundation.IAsyncOperationWithProgress<uint, uint> WriteAsync(IBuffer buffer)
        {
            return this.GetOutputStreamAt(this.Position).WriteAsync(buffer);
        }

        public string ContentType
        {
            get { throw new NotImplementedException(); }
        }
    }
#endif 
}
