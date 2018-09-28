// -----------------------------------------------------------------------------------------
// <copyright file="IExtendedRetryPolicy.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.RetryPolicies
{
    /// <summary>
    /// Represents a retry policy.
    /// </summary>
    public interface IExtendedRetryPolicy : IRetryPolicy
    {
        /// <summary>
        /// Determines whether the operation should be retried and the interval until the next retry.
        /// </summary>
        /// <param name="retryContext">A <see cref="RetryContext"/> object that indicates the number of retries, the results of the last request, and whether the next retry should happen in the primary or secondary location, and specifies the location mode.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="RetryInfo"/> object that indicates the location mode, and whether the next retry should happen in the primary or secondary location. If <c>null</c>, the operation will not be retried.</returns>
        RetryInfo Evaluate(RetryContext retryContext, OperationContext operationContext);
    }
}
