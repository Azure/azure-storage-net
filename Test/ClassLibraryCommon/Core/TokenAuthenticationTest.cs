// -----------------------------------------------------------------------------------------
// <copyright file="StorageUriTests.cs" company="Microsoft">
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

using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage.Core
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Queue;
    using Microsoft.WindowsAzure.Storage.Table;
    using Microsoft.WindowsAzure.Storage.File;
    using System;
    using System.Collections.Generic;
    using System.Net;

#if WINDOWS_DESKTOP
    using Microsoft.VisualStudio.TestTools.UnitTesting;

#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

    [TestClass]
    public class TokenAuthenticationTest : TestBase
    {
        /// <summary>
        /// This is the test state used by the periodic token updater.
        /// It can be anything in practice, in our test case it is simply a string holder.
        /// </summary>
        private class TestState
        {
            public string Buffer { get; set; }

            public TestState(string buffer)
            {
                this.Buffer = buffer;
            }
        }

        /// <summary>
        /// This is the fast token updater.
        /// It simply appends '0' to the current token.
        /// </summary>
        private static Task<NewTokenAndFrequency> FastTokenUpdater(Object state, CancellationToken cancellationToken)
        {
            return
                Task<NewTokenAndFrequency>.Factory.StartNew(
                    () =>
                    {
                        TestState testState = (TestState)state;
                        testState.Buffer += "0";
                        return new NewTokenAndFrequency(testState.Buffer, TimeSpan.FromSeconds(5));
                    }, cancellationToken);
        }

        /// <summary>
        /// This is the super slow token updater. It simulates situations where a token needs to be retrieved from a potato server.
        /// It waits for 10 seconds and then simply appends '0' to the current token.
        /// </summary>
        private static Task<NewTokenAndFrequency> SlowTokenUpdater(Object state, CancellationToken cancellationToken)
        {
            return
                Task<NewTokenAndFrequency>.Factory.StartNew(
                    () =>
                    {
                        TestState testState = (TestState)state;
                        testState.Buffer += "0";
                        Task.Delay(TimeSpan.FromSeconds(10), cancellationToken).Wait(cancellationToken);
                        return new NewTokenAndFrequency(testState.Buffer, TimeSpan.FromSeconds(5));
                    }, cancellationToken);
        }

        /// <summary>
        /// This updater throws exceptions. It simulates situations where errors occur while retrieving a token from a potato server.
        /// </summary>
        private static Task<NewTokenAndFrequency> BrokenTokenUpdater(Object state, CancellationToken cancellationToken)
        {
            return
                Task<NewTokenAndFrequency>.Factory.StartNew(
                    () =>
                    {
                        throw new ServerException();
                    }, cancellationToken);
        }

        [TestMethod]
        [Description("Basic timer triggering test.")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.Smoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void TimerShouldTriggerPeriodically()
        {
            // token updater is triggered every 5 seconds
            TestState state = new TestState("0");
            TokenCredential tokenCredential = new TokenCredential("0", FastTokenUpdater, state, TimeSpan.FromSeconds(5));
            
            // make sure the token starts with the right value, t=0
            Assert.AreEqual("0", tokenCredential.Token);

            // wait until timer triggers for the first time and validate token value, t=6
            Task.Delay(TimeSpan.FromSeconds(6)).Wait();
            Assert.AreEqual("00", tokenCredential.Token);

            // wait until timer triggers for the second time and validate token value, t=12
            Task.Delay(TimeSpan.FromSeconds(6)).Wait();
            Assert.AreEqual("000", tokenCredential.Token);

            // stop the time and make sure it does not trigger anymore, t=18
            tokenCredential.Dispose();
            Task.Delay(TimeSpan.FromSeconds(6)).Wait();
            Assert.AreEqual("000", tokenCredential.Token);
        }

        [TestMethod]
        [Description("Make sure the token updater only gets triggered after the previous update finishes.")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.Smoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void UpdaterShouldRunOneAtATime()
        {
            // token updater is triggered every 5 seconds
            // however, the slow updater takes 10 seconds to provide a new token
            TestState state = new TestState("0");
            TokenCredential tokenCredential = new TokenCredential("0", SlowTokenUpdater, state, TimeSpan.FromSeconds(5));

            // make sure the token starts with the right value, t=0
            Assert.AreEqual("0", tokenCredential.Token);

            // check on the token while updater is running, t=6
            Task.Delay(TimeSpan.FromSeconds(6)).Wait();
            Assert.AreEqual("0", tokenCredential.Token);

            // check on the token after updater is done for the first time, t=16
            // the first updater should have finished at t=15
            Task.Delay(TimeSpan.FromSeconds(10)).Wait();
            Assert.AreEqual("00", tokenCredential.Token);

            // check on the token while updater is running, t=22
            // the second updater should have been triggered at t=20
            Task.Delay(TimeSpan.FromSeconds(6)).Wait();
            Assert.AreEqual("00", tokenCredential.Token);

            // check on the token after updater is done for the second time, t=32
            // the second updater should have finished at t=30
            Task.Delay(TimeSpan.FromSeconds(10)).Wait();
            Assert.AreEqual("000", tokenCredential.Token);

            // stop the timer and make sure it is not triggered anymore, t=50
            tokenCredential.Dispose();
            Task.Delay(TimeSpan.FromSeconds(18)).Wait();
            Assert.AreEqual("000", tokenCredential.Token);
        }

        /// <summary>
        /// TODO: this does not seem to be the desired bahvior, validate with JR.
        /// </summary>
        [TestMethod]
        [Description("Test the situation where the periodic token updater throws an exception.")]
        [TestCategory(ComponentCategory.Core)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.Smoke)]
        [TestCategory(TenantTypeCategory.Cloud)]
        public void ErrorThrownWhenTimerIsTriggered()
        {
            // token updater is triggered every 5 seconds
            TestState state = new TestState("0");
            TokenCredential tokenCredential = new TokenCredential("0", BrokenTokenUpdater, state, TimeSpan.FromSeconds(5));

            // make sure the token starts with the right value, t=0
            Assert.AreEqual("0", tokenCredential.Token);

            // wait until timer triggers for the first time and validate token value, t=6
            Task.Delay(TimeSpan.FromSeconds(6)).Wait();
            Assert.AreEqual("0", tokenCredential.Token);

            // wait until timer triggers for the second time and validate token value, 6=12
            Task.Delay(TimeSpan.FromSeconds(6)).Wait();
            Assert.AreEqual("0", tokenCredential.Token);

            // stop the time and make sure it does not trigger anymore, t=18
            tokenCredential.Dispose();
            Task.Delay(TimeSpan.FromSeconds(6)).Wait();
            Assert.AreEqual("0", tokenCredential.Token);
        }
    }
}