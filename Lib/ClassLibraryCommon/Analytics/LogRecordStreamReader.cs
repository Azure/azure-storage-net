//-----------------------------------------------------------------------
// <copyright file="LogRecordStreamReader.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Analytics
{
    using Microsoft.WindowsAzure.Storage.Core;
    using System;
    using System.IO;
    using System.Text;
    using System.Net;
    using System.Globalization;

    /// <summary>
    /// Reads log record information from a stream.
    /// </summary>
    internal class LogRecordStreamReader : IDisposable
    {
        /// <summary>
        /// A delimiter that exists between fields in a log. 
        /// </summary>
        public const char FieldDelimiter = ';';

        /// <summary>
        /// A delimiter that exists between logs. 
        /// </summary>
        public const char RecordDelimiter = '\n';

        /// <summary>
        /// The quote character.
        /// </summary>
        public const char QuoteChar = '\"';

        private Encoding encoding;

        private StreamReader reader;
        
        private long position;

        private bool isFirstFieldInRecord;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="LogRecordStreamReader"/> class using the specified stream and buffer size.
        /// </summary>
        /// <param name="stream">The <see cref="System.IO.Stream"/> object to read from.</param>
        /// <param name="bufferSize">An integer indicating the size of the buffer.</param>
        public LogRecordStreamReader(Stream stream, int bufferSize)
        {
            this.encoding = new UTF8Encoding(false /* encoderShouldEmitUTF8Identifier */);
            this.reader = new StreamReader(stream, this.encoding, false /* detectEncodingFromByteOrderMarks */, bufferSize);
            this.position = 0;
            this.isFirstFieldInRecord = true;
        }

        /// <summary>
        /// Indicates whether this is the end of the file.
        /// </summary>
        public bool IsEndOfFile
        {
            get
            {
                return this.reader.EndOfStream;
            }
        }

        /// <summary>
        /// Checks the position of the stream.
        /// </summary>
        /// <value>A long containing the current position of the stream.</value>
        public long Position
        {
            get
            {
                return this.position;
            }
        }

        /// <summary>
        /// Checks whether another field exists in the record.
        /// </summary>
        /// <returns>A boolean value indicating whether another field exists.</returns>
        public bool HasMoreFieldsInRecord()
        {
            return this.TryPeekDelimiter(LogRecordStreamReader.FieldDelimiter);
        }

        /// <summary>
        /// Reads a string from the stream.
        /// </summary>
        /// <returns>The string value read from the stream.</returns>
        public string ReadString()
        {
            string temp = this.ReadField(false /* isQuotedString */);

            if (string.IsNullOrEmpty(temp))
            {
                return null;
            }
            else
            {
                return temp;
            }
        }

        /// <summary>
        /// Reads a quoted string from the stream.
        /// </summary>
        /// <returns>The quote string value read from the stream.</returns>
        public string ReadQuotedString()
        {
            string temp = this.ReadField(true /* isQuotedString */);

            if (string.IsNullOrEmpty(temp))
            {
                return null;
            }
            else
            {
                return temp;
            }
        }

        /// <summary> 
        /// Ends the current record by reading the record delimiter and adjusting internal state. 
        /// </summary>
        /// <remarks>The caller is expected to know when the record ends. </remarks>
        public void EndCurrentRecord()
        {
            this.ReadDelimiter(LogRecordStreamReader.RecordDelimiter);
            this.isFirstFieldInRecord = true;
        }

        /// <summary>
        /// Reads a bool from the stream.
        /// </summary>
        /// <returns>The boolean value read from the stream.</returns>
        public bool? ReadBool()
        {
            string temp = this.ReadField(false /* isQuotedString */);

            if (string.IsNullOrEmpty(temp))
            {
                return null;
            }
            else
            {
                return bool.Parse(temp);
            }
        }

        /// <summary>
        /// Reads a <see cref="System.DateTimeOffset"/> value in a specific format from the stream.
        /// </summary>
        /// <param name="format">A string representing the DateTime format to use when parsing.</param>
        /// <returns>The <see cref="System.DateTimeOffset"/> value read.</returns>
        public DateTimeOffset? ReadDateTimeOffset(string format)
        {
            string temp = this.ReadField(false /* isQuotedString */);

            if (string.IsNullOrEmpty(temp))
            {
                return null;
            }
            else
            {
                DateTimeOffset tempDateTime;
                bool parsed = DateTimeOffset.TryParseExact(
                    temp,
                    format,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
                    out tempDateTime);
                if (parsed)
                {
                    return tempDateTime;
                }
                else
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.LogStreamParseError, temp, format));
                }
            }
        }

        /// <summary>
        /// Reads a <see cref="System.TimeSpan"/> value, represented as a number of milliseconds, from the stream.
        /// </summary>
        /// <returns>The <see cref="System.TimeSpan"/> value read from the stream.</returns>
        public TimeSpan? ReadTimeSpanInMS()
        {
            string temp = this.ReadField(false /* isQuotedString */);

            if (string.IsNullOrEmpty(temp))
            {
                return null;
            }
            else
            {
                return new TimeSpan(0, 0, 0, 0, int.Parse(temp, NumberStyles.None, CultureInfo.InvariantCulture));
            }
        }

        /// <summary>
        /// Reads a double from the stream.
        /// </summary>
        /// <returns>The double value read from the stream.</returns>
        public double? ReadDouble()
        {
            string temp = this.ReadField(false /* isQuotedString */);

            if (string.IsNullOrEmpty(temp))
            {
                return null;
            }
            else
            {
                return double.Parse(temp, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>Reads a GUID value from the stream. </summary>
        /// <returns>The <see cref="System.Guid"/> value read from the stream.</returns>
        public Guid? ReadGuid()
        {
            string temp = this.ReadField(false /* isQuotedString */);

            if (string.IsNullOrEmpty(temp))
            {
                return null;
            }
            else
            {
                return Guid.ParseExact(temp, "D");
            }
        }

        /// <summary>Reads an integer value from the stream. </summary>
        /// <returns>The integer value read from the stream.</returns>
        public int? ReadInt()
        {
            string temp = this.ReadField(false /* isQuotedString */);

            if (string.IsNullOrEmpty(temp))
            {
                return null;
            }
            else
            {
                return int.Parse(temp, NumberStyles.None, CultureInfo.InvariantCulture);
            }
        }

        /// <summary> Reads a long value from the stream. </summary>
        /// <returns>The long value read from the stream.</returns>
        public long? ReadLong()
        {
            string temp = this.ReadField(false /* isQuotedString */);

            if (string.IsNullOrEmpty(temp))
            {
                return null;
            }
            else
            {
                return long.Parse(temp, NumberStyles.None, CultureInfo.InvariantCulture);
            }
        }

        /// <summary>
        /// Read a Uri from the stream.
        /// </summary>
        /// <returns>The <see cref="System.Uri"/> object read from the stream.</returns>
        public Uri ReadUri()
        {
            string temp = this.ReadField(true /* isQuotedString */);

            if (string.IsNullOrEmpty(temp))
            {
                return null;
            }
            else
            {
                return new Uri(WebUtility.HtmlDecode(temp));
            }
        }

        private void ReadDelimiter(char delimiterToRead)
        {
            this.EnsureNotEndOfFile();

            long current = this.position;
            int temp = this.reader.Read();
            if (temp == -1
                || (char)temp != delimiterToRead)
            {
                throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.LogStreamDelimiterError, delimiterToRead, (char)temp, current));   
            }

            // Expected delimiter read
            this.position++;
        }

        private bool TryPeekDelimiter(char delimiterToRead)
        {
            this.EnsureNotEndOfFile();

            int temp = this.reader.Peek();
            if (temp == -1
                || (char)temp != delimiterToRead)
            {
                return false;
            }

            return true;
        }

        private void EnsureNotEndOfFile()
        {
            if (this.IsEndOfFile)
            {
                throw new EndOfStreamException(string.Format(CultureInfo.InvariantCulture, SR.LogStreamEndError, this.Position));
            }
        }

        private string ReadField(bool isQuotedString)
        {
            if (!this.isFirstFieldInRecord)
            {
                this.ReadDelimiter(LogRecordStreamReader.FieldDelimiter);
            }
            else
            {
                this.isFirstFieldInRecord = false;
            }

            // Read a field, handling field/record delimiters in quotes and not counting them,
            // and also check that there are no record delimiters since they are only expected
            // outside of a field.
            // Note: We only need to handle strings that are quoted once from the beginning,
            // (e.g. "mystring"). We do not need to handle nested quotes or anything because
            // we control the string format.
            StringBuilder fieldBuilder = new StringBuilder();
            bool hasSeenQuoteForQuotedString = false;
            bool isExpectingDelimiterForNextCharacterForQuotedString = false;
            while (true)
            {
                // If EOF when we have not read any delimiter char, this is unexpected.
                this.EnsureNotEndOfFile();

                char c = (char)this.reader.Peek();

                // If we hit a delimiter that is not quoted or we hit the delimiter for
                // a quoted string or we hit the empty value string and hit a delimiter,
                // then we have finished reading the field.
                // Note: The empty value string is the only string that we don't require
                // quotes for for a quoted string.
                if ((!isQuotedString
                     || isExpectingDelimiterForNextCharacterForQuotedString
                     || fieldBuilder.Length == 0)
                    && (c == LogRecordStreamReader.FieldDelimiter || c == LogRecordStreamReader.RecordDelimiter))
                {
                    // Note: We only peeked this character, so it has not yet
                    // been consumed. Field delimiters will be consumed on the
                    // next ReadField call; record delimiters will be consumed
                    // on a call to EndCurrentRecord.
                    break;
                }

                if (isExpectingDelimiterForNextCharacterForQuotedString)
                {
                    // We finished reading a quoted string, but the next character after
                    // the closing quote was not a delimiter, which is not expected.
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.LogStreamQuoteError, fieldBuilder.ToString(), c));
                }

                // The character was not a delimiter, so consume it and
                // add it to our field string
                this.reader.Read();
                fieldBuilder.Append(c);
                this.position++;

                // We need to handle quotes specially since quoted delimiters
                // do not count since they are considered to be part of the
                // quoted string and not actually a delimiter.
                // Note: We use a specific quote character since we control the format
                // and we only allow non-encoded quote characters at the beginning/end
                // of the string.
                if (c == LogRecordStreamReader.QuoteChar)
                {
                    if (!isQuotedString)
                    {
                        // The quote character non-encoded is only allowed for quoted strings
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.LogStreamQuoteError, fieldBuilder.ToString(), LogRecordStreamReader.QuoteChar));
                    }
                    else if (fieldBuilder.Length == 1)
                    {
                        // This is the opening quote for a quoted string
                        hasSeenQuoteForQuotedString = true;
                    }
                    else if (hasSeenQuoteForQuotedString)
                    {
                        // This is the closing quote for a quoted string
                        isExpectingDelimiterForNextCharacterForQuotedString = true;
                    }
                    else
                    {
                        // We encountered an unexpected non-encoded quote character
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, SR.LogStreamQuoteError, fieldBuilder.ToString(), LogRecordStreamReader.QuoteChar));
                    }
                }
            }

            string field;
            
            // Note: For quoted strings we remove the quotes.
            // We do not do this for the empty value string since it represents empty
            // and we don't write that out in quotes even for quoted strings.
            if (isQuotedString
                && fieldBuilder.Length != 0)
            {
                field = fieldBuilder.ToString(1, fieldBuilder.Length - 2);
            }
            else
            {
                field = fieldBuilder.ToString();
            }

            return field;
        }

#region IDisposable Members
        /// <summary>
        /// Dispose this LogRecordStreamReader.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Dispose this LogRecordStreamReader
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.reader.Close();
            }
        }

        ~LogRecordStreamReader()
        {
            this.Dispose(false);
        }
#endregion
    }
}