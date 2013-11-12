// -----------------------------------------------------------------------------------------
// <copyright file="AlwaysRetry.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core
{
    using Microsoft.WindowsAzure.Storage.RetryPolicies;
    using System;
    using System.Collections.Generic;

#if WINDOWS_DESKTOP
    using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
    using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

    public class AlwaysRetry : IExtendedRetryPolicy
    {
        private IList<RetryContext> retryContextList;
        private IList<RetryInfo> retryInfoList;
        private int retryCount = 0;

        public AlwaysRetry(IEnumerable<RetryContext> retryContextList, IEnumerable<RetryInfo> retryInfoList)
        {
            this.retryContextList = new List<RetryContext>(retryContextList);
            this.retryInfoList = new List<RetryInfo>(retryInfoList);
            Assert.AreEqual(this.retryContextList.Count, this.retryInfoList.Count + 1);
        }

        public bool ShouldRetry(int currentRetryCount, int statusCode, Exception lastException, out TimeSpan retryInterval, OperationContext operationContext)
        {
            Assert.AreEqual(this.retryCount++, currentRetryCount);

            if (currentRetryCount < this.retryInfoList.Count)
            {
                retryInterval = this.retryInfoList[currentRetryCount].RetryInterval;
                return true;
            }
            else
            {
                retryInterval = TimeSpan.Zero;
                return false;
            }
        }

        public RetryInfo Evaluate(RetryContext retryContext, OperationContext operationContext)
        {
            Assert.IsTrue(retryContext.CurrentRetryCount < this.retryContextList.Count, "Executor should not try to evaluate more retries after we return null");

            Assert.AreEqual(this.retryContextList[retryContext.CurrentRetryCount].NextLocation, retryContext.NextLocation);
            Assert.AreEqual(this.retryContextList[retryContext.CurrentRetryCount].LocationMode, retryContext.LocationMode);

            TimeSpan retryInterval;
            if (this.ShouldRetry(retryContext.CurrentRetryCount, retryContext.LastRequestResult.HttpStatusCode, retryContext.LastRequestResult.Exception, out retryInterval, operationContext))
            {
                return this.retryInfoList[retryContext.CurrentRetryCount];
            }

            return null;
        }

        public IRetryPolicy CreateInstance()
        {
            return new AlwaysRetry(this.retryContextList, this.retryInfoList);
        }
    }
}
