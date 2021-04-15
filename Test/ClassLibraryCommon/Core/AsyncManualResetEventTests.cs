// -----------------------------------------------------------------------------------------
// <copyright file="AsyncStreamCopierTests.cs" company="Microsoft">
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

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Core.Util;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Storage.Core
{
    [TestClass]
    public class AsyncManualResetEventTests
    {
        [TestMethod]
        public void CtorCreateSet()
        {
            // arrange
            var theEvent = new AsyncManualResetEvent(true);

            // act
            bool completed = theEvent.WaitAsync().Wait(TimeSpan.FromSeconds(2));

            // assert
            Assert.IsTrue(completed);
        }

        [TestMethod]
        public void CtorCreateUnSet()
        {
            // arrange
            var theEvent = new AsyncManualResetEvent(false);

            // act
            bool completed = theEvent.WaitAsync().Wait(TimeSpan.FromSeconds(2));

            // assert
            Assert.IsFalse(completed);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void ShouldReset(bool initialState)
        {
            // arrange
            var theEvent = new AsyncManualResetEvent(initialState);

            // act
            theEvent.Reset();

            // assert
            bool completed = theEvent.WaitAsync().Wait(TimeSpan.FromSeconds(2));
            Assert.IsFalse(completed);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ShouldResetAfterSequenceOfTransitions(bool initialState)
        {
            // arrange
            var theEvent = new AsyncManualResetEvent(initialState);

            // act
            await theEvent.Set();
            theEvent.Reset();
            await theEvent.Set();
            theEvent.Reset();

            // assert
            bool completed = theEvent.WaitAsync().Wait(TimeSpan.FromSeconds(2));
            Assert.IsFalse(completed);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ShouldSet(bool initialState)
        {
            // arrange
            var theEvent = new AsyncManualResetEvent(initialState);

            // act
            await theEvent.Set();

            // assert
            bool completed = theEvent.WaitAsync().Wait(TimeSpan.FromSeconds(2));
            Assert.IsTrue(completed);
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public async Task ShouldSetAfterSequenceOfTransitions(bool initialState)
        {
            // arrange
            var theEvent = new AsyncManualResetEvent(initialState);

            // act
            await theEvent.Set();
            theEvent.Reset();
            await theEvent.Set();
            theEvent.Reset();
            await theEvent.Set();

            // assert
            bool completed = theEvent.WaitAsync().Wait(TimeSpan.FromSeconds(2));
            Assert.IsTrue(completed);
        }
    }

}