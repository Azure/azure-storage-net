// -----------------------------------------------------------------------------------------
// <copyright file="TestHelper.cs" company="Microsoft">
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

using Fiddler;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Test.Network;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.WindowsAzure.Storage
{
    public partial class TestHelper
    {
        /// <summary>
        /// Runs a given operation that is expected to throw an exception.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="operationDescription"></param>
        /// <param name="expectedStatusCode"></param>
        internal static void ExpectedException(Action operation, string operationDescription, HttpStatusCode expectedStatusCode, string requestErrorCode = null)
        {
            try
            {
                operation();
            }
            catch (StorageException ex)
            {
                Assert.AreEqual((int)expectedStatusCode, ex.RequestInformation.HttpStatusCode, "Http status code is unexpected.");
                if (!string.IsNullOrEmpty(requestErrorCode))
                {
                    Assert.IsNotNull(ex.RequestInformation.ExtendedErrorInformation);
                    Assert.AreEqual(requestErrorCode, ex.RequestInformation.ExtendedErrorInformation.ErrorCode);
                }
                return;
            }

            Assert.Fail("No Storage exception received while expecting {0}: {1}", expectedStatusCode, operationDescription);
        }

#if TASK
        /// <summary>
        /// Runs a given operation that is expected to throw an exception.
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="operationDescription"></param>
        /// <param name="expectedStatusCode"></param>
        internal static void ExpectedExceptionTask(Task operation, string operationDescription, HttpStatusCode expectedStatusCode, string requestErrorCode = null)
        {
            try
            {
                operation.Wait();
            }
            catch (AggregateException e)
            {
                e = e.Flatten();
                if (e.InnerExceptions.Count > 1)
                {
                    Assert.Fail("Multiple exception received while expecting {0}: {1}", expectedStatusCode, operationDescription);
                }

                StorageException ex = e.InnerException as StorageException;
                if (ex == null)
                {
                    throw e.InnerException;
                }

                Assert.AreEqual((int)expectedStatusCode, ex.RequestInformation.HttpStatusCode, "Http status code is unexpected.");
                if (!string.IsNullOrEmpty(requestErrorCode))
                {
                    Assert.IsNotNull(ex.RequestInformation.ExtendedErrorInformation);
                    Assert.AreEqual(requestErrorCode, ex.RequestInformation.ExtendedErrorInformation.ErrorCode);
                }
                return;
            }

            Assert.Fail("No exception received while expecting {0}: {1}", expectedStatusCode, operationDescription);
        }
#endif

#if WINDOWS_DESKTOP && !WINDOWS_PHONE
        internal static void ExecuteAPMMethodWithCancellation(int cancellationDelayInMS,
          ProxyBehavior[] behaviors,
          Func<IRequestOptions,
          OperationContext,
          AsyncCallback,
          object,
          ICancellableAsyncResult> begin,
          Action<IAsyncResult> end)
        {
            ExecuteAPMMethodWithCancellation<bool>(cancellationDelayInMS,
                behaviors,
                begin,
                (res) =>
                {
                    end(res);
                    return true;
                });
        }

        internal static void ExecuteAPMMethodWithCancellation<T>(int cancellationDelayInMS,
            ProxyBehavior[] behaviors,
            Func<IRequestOptions,
            OperationContext,
            AsyncCallback,
            object,
            ICancellableAsyncResult> begin,
            Func<IAsyncResult, T> end)
        {
            string failMessage = null;
            StorageException storageException = null;
            OperationContext opContext = new OperationContext();

            using (HttpMangler proxy = new HttpMangler(false, behaviors))
            {
                Debug.WriteLine("Begin");
                using (ManualResetEvent completedEvent = new ManualResetEvent(false))
                {
                    ICancellableAsyncResult saveResult = begin(null
                        , opContext,
                        (resp) =>
                        {
                            try
                            {
                                end(resp);
                                failMessage = "Request succeeded even after cancellation";
                            }
                            catch (StorageException ex)
                            {
                                storageException = ex;
                            }
                            catch (Exception badEx)
                            {
                                failMessage = badEx.ToString();
                            }
                            finally
                            {
                                completedEvent.Set();
                            }
                        },
                    null);

                    Thread.Sleep(cancellationDelayInMS);
                    Debug.WriteLine("Cancelling Request");
                    saveResult.Cancel();

                    completedEvent.WaitOne();
                    TestHelper.AssertNAttempts(opContext, 1);
                }
            }

            // Do not use IsNull here so that test result contains failMessage
            Assert.AreEqual(null, failMessage);

            Assert.IsNotNull(storageException);
            Assert.AreEqual("Operation was canceled by user.", storageException.Message);
            Assert.AreEqual(306, storageException.RequestInformation.HttpStatusCode);
            Assert.AreEqual("Unused", storageException.RequestInformation.HttpStatusMessage);
        }

        internal static void ExecuteAPMMethodWithRetry<T>(int expectedAttempts,
            ProxyBehavior[] behaviors,
            Func<IRequestOptions, OperationContext, AsyncCallback, object, ICancellableAsyncResult> begin,
            Func<IAsyncResult, T> end)
        {
            string failMessage = null;
            OperationContext opContext = new OperationContext();

            using (HttpMangler proxy = new HttpMangler(false, behaviors))
            {
                using (ManualResetEvent completedEvent = new ManualResetEvent(false))
                {
                    ICancellableAsyncResult saveResult = begin(null,
                        opContext,
                        (resp) =>
                        {
                            try
                            {
                                end(resp);
                            }
                            catch (Exception badEx)
                            {
                                failMessage = badEx.ToString();
                            }
                            finally
                            {
                                completedEvent.Set();
                            }
                        },
                        null);

                    completedEvent.WaitOne();
                    TestHelper.AssertNAttempts(opContext, expectedAttempts);
                }
            }

            // Do not use IsNull here so that test result contains failMessage
            Assert.AreEqual(null, failMessage);
        }

#if TASK
        internal static void ExecuteTaskMethodWithRetry<T>(int expectedAttempts,
            ProxyBehavior[] behaviors,
            Func<IRequestOptions, OperationContext, Task<T>> method)
        {
            OperationContext opContext = new OperationContext();

            using (HttpMangler proxy = new HttpMangler(false, behaviors))
            {
                method(null, opContext).Wait();
                TestHelper.AssertNAttempts(opContext, expectedAttempts);
            }
        }
#endif

        internal static void ExecuteMethodWithRetry<T>(int expectedAttempts,
            ProxyBehavior[] behaviors,
            Func<IRequestOptions, OperationContext, T> method)
        {
            OperationContext opContext = new OperationContext();

            using (HttpMangler proxy = new HttpMangler(false, behaviors))
            {
                method(null, opContext);
                TestHelper.AssertNAttempts(opContext, expectedAttempts);
            }
        }

        internal static void ExecuteMethodWithRetryInTryFinally<T>(int expectedAttempts,
            ProxyBehavior[] behaviors,
            Func<IRequestOptions, OperationContext, T> method)
        {
            OperationContext opContext = new OperationContext();

            using (HttpMangler proxy = new HttpMangler(false, behaviors))
            {
                try
                {
                    method(null, opContext);
                }
                finally
                {
                    TestHelper.AssertNAttempts(opContext, expectedAttempts);
                }
            }
        }

        internal static void VerifyHeaderWasSent(string headerName, string headerValue, Func<Session, bool> selector, Action act)
        {
            string retrievedHeaderValue = null;
            using (HttpMangler proxy = new HttpMangler(false, new ProxyBehavior[]{ new ProxyBehavior(session =>
                {
                    retrievedHeaderValue = session.oRequest.headers[headerName];
                }, selector, null, TriggerType.BeforeRequest)}))
            {
                act();
            }

            Assert.AreEqual(headerValue, retrievedHeaderValue);
        }

        internal static void ValidateIngressEgress(Func<Session, bool> selector, Func<RequestResult> act)
        {
            RequestResult res = null;
            long observedIngressBodyBytes = 0;
            long observedEgressBodyBytes = 0;
            bool isChunked = false;

            using (HttpMangler proxy = new HttpMangler(false, new ProxyBehavior[]{
                new ProxyBehavior(session => observedEgressBodyBytes += session.requestBodyBytes.Length, selector, null, TriggerType.BeforeRequest),
                new ProxyBehavior(session =>
                {
                    isChunked = session.oResponse.headers.ExistsAndContains("Transfer-Encoding", "chunked");
                    observedIngressBodyBytes += session.responseBodyBytes.Length;
                }, selector, null, TriggerType.AfterSessionComplete),
            }))
            {
                res = act();
            }

            Assert.IsNotNull(res);

            // If chunked use more lenient evaluation
            if (isChunked)
            {
                Assert.IsTrue(res.IngressBytes < observedIngressBodyBytes);

                // 5 bytes for chunked encoded 
                if (res.IngressBytes == 0)
                {
                    Assert.IsTrue(observedIngressBodyBytes == 5);
                }
            }
            else
            {
                Assert.AreEqual(res.IngressBytes, observedIngressBodyBytes);
            }

            Assert.AreEqual(res.EgressBytes, observedEgressBodyBytes);
        }
#endif
    }
}
