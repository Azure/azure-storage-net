// -----------------------------------------------------------------------------------------
// <copyright file="TestHelper.Common.cs" company="Microsoft">
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

using System.Threading;

namespace Microsoft.Azure.Storage
{
    public class MockBufferManager : IBufferManager
    {
        private int defaultBufferSize = 0;
        private int totalTakeBufferCalls = 0;

        public MockBufferManager(int defaultBufferSize)
        {
            this.defaultBufferSize = defaultBufferSize;
        }

        private int outstandingBufferCount = 0;
        public int OutstandingBufferCount
        {
            get
            {
                return Interlocked.CompareExchange(ref outstandingBufferCount, 0, 0);
            }
            set
            {
                Interlocked.Exchange(ref outstandingBufferCount, value);
            }
        }

        public void ReturnBuffer(byte[] buffer)
        {
            Interlocked.Decrement(ref outstandingBufferCount);
            // no op
        }

        public byte[] TakeBuffer(int bufferSize)
        {
            Interlocked.Increment(ref outstandingBufferCount);
            Interlocked.Increment(ref totalTakeBufferCalls);
            return new byte[bufferSize];
        }

        public int GetDefaultBufferSize()
        {
            return this.defaultBufferSize;
        }

        public int TotalTakeBufferCalls
        {
            get
            {
                return this.totalTakeBufferCalls;
            }
        }
    }
}
