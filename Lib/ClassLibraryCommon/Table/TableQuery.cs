// -----------------------------------------------------------------------------------------
// <copyright file="TableQuery.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Table
{
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Executor;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table.Protocol;
    using Microsoft.WindowsAzure.Storage.Table.Queryable;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents a query against a Microsoft Azure table.
    /// </summary>
    /// <typeparam name="TElement">A class which implements <see cref="ITableEntity"/>.</typeparam>
    public partial class TableQuery<TElement> : IQueryable<TElement>
    {
        #region Private fields.

        private readonly Expression queryExpression;

        private readonly TableQueryProvider queryProvider;

        #endregion

        #region Ctors

        /// <summary>
        /// Initializes a new instance of the <see cref="TableQuery{TElement}"/> class.
        /// </summary>
        public TableQuery()
        {
            // Instantiated by user, validate that Telement implements ITableEnity
            if (typeof(TElement).GetInterface(typeof(ITableEntity).FullName, false) == null)
            {
                throw new NotSupportedException(SR.TableQueryTypeMustImplementITableEnitty);
            }

            if (typeof(TElement).GetConstructor(System.Type.EmptyTypes) == null)
            {
                throw new NotSupportedException(SR.TableQueryTypeMustHaveDefaultParameterlessCtor);
            }
        }

        // used by client to create the first query
        internal TableQuery(CloudTable table)
            : base()
        {
            this.queryProvider = new TableQueryProvider(table);

            // TODO can base expression be non constant?
            this.queryExpression =
                new ResourceSetExpression(typeof(IOrderedQueryable<TElement>), null, Expression.Constant("0"), typeof(TElement), null, CountOption.None, null, null);
        }

        // Used by iqueryable on subsequent expression updates to update expression / provider
        internal TableQuery(Expression queryExpression, TableQueryProvider queryProvider)
        {
            this.queryProvider = queryProvider;
            this.queryExpression = queryExpression;
        }

        #endregion

        #region Public Execution Methods

        /// <summary>
        /// Executes a query on a table.
        /// </summary>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection, specialized for type <c>TElement</c>, of the results of executing the query.</returns>       
        [DoesServiceRequest]
        public virtual IEnumerable<TElement> Execute(TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            if (this.queryProvider == null)
            {
                throw new InvalidOperationException(SR.TableQueryMustHaveQueryProvider);
            }

            ExecutionInfo executionInfo = this.Bind();
            executionInfo.RequestOptions = requestOptions ?? executionInfo.RequestOptions;
            executionInfo.OperationContext = operationContext ?? executionInfo.OperationContext;

            if (executionInfo.Resolver != null)
            {
                // Execute the query. 
                return this.ExecuteInternal<TElement>(this.queryProvider.Table.ServiceClient, this.queryProvider.Table, executionInfo.Resolver, executionInfo.RequestOptions, executionInfo.OperationContext);
            }
            else
            {
                return this.ExecuteInternal(this.queryProvider.Table.ServiceClient, this.queryProvider.Table, executionInfo.RequestOptions, executionInfo.OperationContext);
            }
        }

        /// <summary>
        /// Begins an asynchronous operation to execute a query and return the results as a result segment.
        /// </summary>
        /// <param name="currentToken">A <see cref="TableContinuationToken"/> object returned by a previous listing operation. May be <c>null</c>.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecuteSegmented(TableContinuationToken currentToken, AsyncCallback callback, object state)
        {
            return this.BeginExecuteSegmented(currentToken, null /* RequestOptions */, null /* OperationContext */, callback, state);
        }

        /// <summary>
        /// Begins an asynchronous operation to execute a query and return the results as a result segment.
        /// </summary>
        /// <param name="currentToken">A <see cref="TableContinuationToken"/> object returned by a previous listing operation. May be <c>null</c>.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="callback">An <see cref="AsyncCallback"/> delegate that will receive notification when the asynchronous operation completes.</param>
        /// <param name="state">A user-defined object that will be passed to the callback delegate.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>An <see cref="IAsyncResult"/> that references the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual ICancellableAsyncResult BeginExecuteSegmented(TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            if (this.queryProvider == null)
            {
                throw new InvalidOperationException(SR.TableQueryMustHaveQueryProvider);
            }

            ExecutionInfo executionInfo = this.Bind();
            executionInfo.RequestOptions = requestOptions == null ? executionInfo.RequestOptions : requestOptions;
            executionInfo.OperationContext = operationContext == null ? executionInfo.OperationContext : operationContext;

            if (executionInfo.Resolver != null)
            {
                // Execute the query. 
                return this.BeginExecuteQuerySegmentedInternal(
                                                        currentToken,
                                                        this.queryProvider.Table.ServiceClient,
                                                        this.queryProvider.Table,
                                                        executionInfo.Resolver,
                                                        executionInfo.RequestOptions,
                                                        executionInfo.OperationContext,
                                                        callback,
                                                        state);
            }
            else
            {
                return this.BeginExecuteQuerySegmentedInternal(
                                                        currentToken,
                                                        this.queryProvider.Table.ServiceClient,
                                                        this.queryProvider.Table,
                                                        executionInfo.RequestOptions,
                                                        executionInfo.OperationContext,
                                                        callback,
                                                        state);
            }
        }

        /// <summary>
        /// Ends an asynchronous operation to execute a query and return the results as a result segment.
        /// </summary>
        /// <param name="asyncResult">The reference to the pending asynchronous request to finish.</param>
        /// <returns>A <see cref="TableQuerySegment{TElement}"/> object of type <typeparamref name="TElement"/>.</returns>
        public virtual TableQuerySegment<TElement> EndExecuteSegmented(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<TableQuerySegment<TElement>>(asyncResult);
        }

#if TASK
        /// <summary>
        /// Initiates an asynchronous operation to execute a query and return the results as a result segment.
        /// </summary>
        /// <param name="currentToken">A continuation token returned by a previous listing operation, can be null.</param>
        /// <returns>A <see cref="Task{T}"/> of type <see cref="TableQuerySegment{TElement}"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TElement>> ExecuteSegmentedAsync(TableContinuationToken currentToken)
        {
            return this.ExecuteSegmentedAsync(currentToken, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to execute a query and return the results as a result segment.
        /// </summary>
        /// <param name="currentToken">A continuation token returned by a previous listing operation, can be null.</param>/// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> of type <see cref="TableQuerySegment{TElement}"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TElement>> ExecuteSegmentedAsync(TableContinuationToken currentToken, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecuteSegmented, this.EndExecuteSegmented, currentToken, cancellationToken);
        }

        /// <summary>
        /// Initiates an asynchronous operation to execute a query and return the results as a result segment.
        /// </summary>
        /// <param name="currentToken">A continuation token returned by a previous listing operation, can be null.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <returns>A <see cref="Task{T}"/> of type <see cref="TableQuerySegment{TElement}"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TElement>> ExecuteSegmentedAsync(TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            return this.ExecuteSegmentedAsync(currentToken, requestOptions, operationContext, CancellationToken.None);
        }

        /// <summary>
        /// Initiates an asynchronous operation to execute a query and return the results as a result segment.
        /// </summary>
        /// <param name="currentToken">A continuation token returned by a previous listing operation, can be null.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for a task to complete.</param>
        /// <returns>A <see cref="Task{T}"/> of type <see cref="TableQuerySegment{TElement}"/> that represents the asynchronous operation.</returns>
        [DoesServiceRequest]
        public virtual Task<TableQuerySegment<TElement>> ExecuteSegmentedAsync(TableContinuationToken currentToken, TableRequestOptions requestOptions, OperationContext operationContext, CancellationToken cancellationToken)
        {
            return AsyncExtensions.TaskFromApm(this.BeginExecuteSegmented, this.EndExecuteSegmented, currentToken, requestOptions, operationContext, cancellationToken);
        }
#endif

#if SYNC
        /// <summary>
        /// Executes a segmented query against a table.
        /// </summary>
        /// <param name="continuationToken">A <see cref="TableContinuationToken"/> object representing a continuation token from the server when the operation returns a partial result.</param>
        /// <param name="requestOptions">A <see cref="TableRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>A <see cref="TableQuerySegment{TElement}"/>, specialized for type <c>TElement</c>, containing the results of executing the query.</returns>
        [DoesServiceRequest]
        public virtual TableQuerySegment<TElement> ExecuteSegmented(TableContinuationToken continuationToken, TableRequestOptions requestOptions = null, OperationContext operationContext = null)
        {
            if (this.queryProvider == null)
            {
                throw new InvalidOperationException(SR.TableQueryMustHaveQueryProvider);
            }

            ExecutionInfo executionInfo = this.Bind();
            executionInfo.RequestOptions = requestOptions == null ? executionInfo.RequestOptions : requestOptions;
            executionInfo.OperationContext = operationContext == null ? executionInfo.OperationContext : operationContext;

            if (executionInfo.Resolver != null)
            {
                // Execute the query. 
                return this.ExecuteQuerySegmentedInternal<TElement>(continuationToken, this.queryProvider.Table.ServiceClient, this.queryProvider.Table, executionInfo.Resolver, executionInfo.RequestOptions, executionInfo.OperationContext);
            }
            else
            {
                return this.ExecuteQuerySegmentedInternal(continuationToken, this.queryProvider.Table.ServiceClient, this.queryProvider.Table, executionInfo.RequestOptions, executionInfo.OperationContext);
            }
        }

#endif
        #endregion

        #region IQueryable implementation
        /// <summary>
        /// Returns an enumerator that iterates through the <see cref="TableQuery{TElement}"/>.
        /// </summary>
        /// <returns>An <see cref="IEnumerator{TElement}"/> for the <see cref="TableQuery{TElement}"/>.</returns>
        public virtual IEnumerator<TElement> GetEnumerator()
        {
            if (this.Expression == null)
            {
                TableRequestOptions defaultRequestOptions = TableRequestOptions.ApplyDefaults(null, this.queryProvider.Table.ServiceClient);

                // TODO should we just throw here? 
                // Standard Query Mode
                return this.ExecuteInternal(this.queryProvider.Table.ServiceClient, this.queryProvider.Table, defaultRequestOptions, null /* OperationContext */).GetEnumerator();
            }
            else
            {
                ExecutionInfo executionInfo = this.Bind();

                if (executionInfo.Resolver != null)
                {
                    // Execute the query. 
                    return this.ExecuteInternal<TElement>(this.queryProvider.Table.ServiceClient, this.queryProvider.Table, executionInfo.Resolver, executionInfo.RequestOptions, executionInfo.OperationContext).GetEnumerator();
                }
                else
                {
                    return this.ExecuteInternal(this.queryProvider.Table.ServiceClient, this.queryProvider.Table, executionInfo.RequestOptions, executionInfo.OperationContext).GetEnumerator();
                }
            }
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        /// <summary>
        /// Gets the type of the element(s) that are returned when the expression tree is executed.
        /// </summary>
        /// <returns>A <see cref="T:System.Type"/> that represents the type of the element(s) that are returned when the expression tree associated with this object is executed.</returns>
        public Type ElementType
        {
            get { return typeof(TElement); }
        }

        /// <summary>
        /// Gets the expression tree.
        /// </summary>
        /// <returns>The <see cref="T:System.Linq.Expressions.Expression"/> that is associated with this instance of <see cref="T:System.Linq.IQueryable"/>.</returns>
        public Expression Expression
        {
            get { return this.queryExpression; }
        }

        /// <summary>
        /// Gets the query provider that is associated with this data source.
        /// </summary>
        /// <returns>The <see cref="T:System.Linq.IQueryProvider"/> that is associated with this data source.</returns>
        public IQueryProvider Provider
        {
            get { return this.queryProvider; }
        }

        internal ExecutionInfo Bind()
        {
            ExecutionInfo retVal = new ExecutionInfo();

            // IQueryable impl
            if (this.Expression != null)
            {
                Dictionary<Expression, Expression> normalizerRewrites = new Dictionary<Expression, Expression>(ReferenceEqualityComparer<Expression>.Instance);

                // Step 1. Evaluate any local evaluatable expressions ( lambdas etc)
                Expression partialEvaluatedExpression = Evaluator.PartialEval(this.Expression);

                // Step 2. Normalize expression, replace String Comparisons etc.
                Expression normalizedExpression = ExpressionNormalizer.Normalize(partialEvaluatedExpression, normalizerRewrites);

                // Step 3. Bind Expression, Analyze predicates and create query option expressions. End result is a single ResourceSetExpression
                Expression boundExpression = ResourceBinder.Bind(normalizedExpression);

                // Step 4. Parse the Bound expression into sub components, i.e. take count, filter, select columns, request options, opcontext, etc.
                ExpressionParser parser = new ExpressionParser();
                parser.Translate(boundExpression);

                // Step 5. Store query components & params
                this.TakeCount = parser.TakeCount;
                this.FilterString = parser.FilterString;
                this.SelectColumns = parser.SelectColumns;
                retVal.RequestOptions = parser.RequestOptions;
                retVal.OperationContext = parser.OperationContext;

                // Step 6. If projection & no resolver then generate a resolver to perform the projection
                if (parser.Resolver == null)
                {
                    if (parser.Projection != null && parser.Projection.Selector != ProjectionQueryOptionExpression.DefaultLambda)
                    {
                        Type intermediateType = parser.Projection.Selector.Parameters[0].Type;

                        // Convert Expression to take type object as input to allow for direct invocation. 
                        ParameterExpression paramExpr = Expression.Parameter(typeof(object));

                        Func<object, TElement> projectorFunc = Expression.Lambda<Func<object, TElement>>(
                            Expression.Invoke(parser.Projection.Selector, Expression.Convert(paramExpr, intermediateType)), paramExpr).Compile();

                        // Generate a resolver to do the projection.
                        retVal.Resolver = (pk, rk, ts, props, etag) =>
                        {
                            // Parse to intermediate type                      
                            ITableEntity intermediateObject = (ITableEntity)EntityUtilities.InstantiateEntityFromType(intermediateType);
                            intermediateObject.PartitionKey = pk;
                            intermediateObject.RowKey = rk;
                            intermediateObject.Timestamp = ts;
                            intermediateObject.ReadEntity(props, parser.OperationContext);
                            intermediateObject.ETag = etag;

                            // Invoke lambda expression
                            return projectorFunc(intermediateObject);
                        };
                    }
                    else
                    {
                        // No op - No resolver or projection specified.
                    }
                }
                else
                {
                    retVal.Resolver = (EntityResolver<TElement>)parser.Resolver.Value;
                }
            }

            retVal.RequestOptions = TableRequestOptions.ApplyDefaults(retVal.RequestOptions, this.queryProvider.Table.ServiceClient);
            retVal.OperationContext = retVal.OperationContext ?? new OperationContext();
            return retVal;
        }

        internal class ExecutionInfo
        {
            public OperationContext OperationContext { get; set; }

            public TableRequestOptions RequestOptions { get; set; }

            public EntityResolver<TElement> Resolver { get; set; }
        }
        #endregion

        #region Internal Impl
        
        internal IEnumerable<TElement> ExecuteInternal(CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            IEnumerable<TElement> enumerable = CommonUtility.LazyEnumerable<TElement>(
                (continuationToken) =>
                {
                    TableQuerySegment<TElement> seg =
#if SYNC
                        this.ExecuteQuerySegmentedInternal((TableContinuationToken)continuationToken, client, table, modifiedOptions, operationContext);
#else
                        this.EndExecuteQuerySegmentedInternal(this.BeginExecuteQuerySegmentedInternal((TableContinuationToken)continuationToken, client, table, modifiedOptions, operationContext, null /* callback */, null /* state */));
#endif
                    return new ResultSegment<TElement>(seg.Results) { ContinuationToken = seg.ContinuationToken };
                },
                this.TakeCount.HasValue ? this.TakeCount.Value : long.MaxValue);

            return enumerable;
        }

#if SYNC
        internal TableQuerySegment<TElement> ExecuteQuerySegmentedInternal(TableContinuationToken token, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            RESTCommand<TableQuerySegment<TElement>> cmdToExecute = QueryImpl(this, token, client, table, EntityUtilities.ResolveEntityByType<TElement>, modifiedOptions);

            return Executor.ExecuteSync(cmdToExecute, modifiedOptions.RetryPolicy, operationContext);
        } 
#endif

        internal ICancellableAsyncResult BeginExecuteQuerySegmentedInternal(TableContinuationToken token, CloudTableClient client, CloudTable table, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);

            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            return Executor.BeginExecuteAsync(
                                          QueryImpl(this, token, client, table, EntityUtilities.ResolveEntityByType<TElement>, modifiedOptions),
                                          modifiedOptions.RetryPolicy,
                                          operationContext,
                                          callback,
                                          state);
        }

        internal TableQuerySegment<TElement> EndExecuteQuerySegmentedInternal(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<TableQuerySegment<TElement>>(asyncResult);
        }

        internal IEnumerable<TResult> ExecuteInternal<TResult>(CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
            CommonUtility.AssertNotNull("resolver", resolver);

            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            IEnumerable<TResult> enumerable = CommonUtility.LazyEnumerable<TResult>(
                (continuationToken) =>
                {
                    TableQuerySegment<TResult> seg =
#if SYNC
                        this.ExecuteQuerySegmentedInternal((TableContinuationToken)continuationToken, client, table, resolver, modifiedOptions, operationContext);
#else
                        this.EndExecuteQuerySegmentedInternal<TResult>(this.BeginExecuteQuerySegmentedInternal((TableContinuationToken)continuationToken, client, table, resolver, modifiedOptions, operationContext, null /* callback */, null /* state */));
#endif
                    return new ResultSegment<TResult>(seg.Results) { ContinuationToken = seg.ContinuationToken };
                },
                this.takeCount.HasValue ? this.takeCount.Value : long.MaxValue);

            return enumerable;
        }

#if SYNC
        internal TableQuerySegment<TResult> ExecuteQuerySegmentedInternal<TResult>(TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
            CommonUtility.AssertNotNull("resolver", resolver);

            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            RESTCommand<TableQuerySegment<TResult>> cmdToExecute = QueryImpl(this, token, client, table, resolver, modifiedOptions);

            return Executor.ExecuteSync(cmdToExecute, modifiedOptions.RetryPolicy, operationContext);
        }
#endif

        internal ICancellableAsyncResult BeginExecuteQuerySegmentedInternal<TResult>(TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<TResult> resolver, TableRequestOptions requestOptions, OperationContext operationContext, AsyncCallback callback, object state)
        {
            CommonUtility.AssertNotNullOrEmpty("tableName", table.Name);
            CommonUtility.AssertNotNull("resolver", resolver);

            TableRequestOptions modifiedOptions = TableRequestOptions.ApplyDefaults(requestOptions, client);
            operationContext = operationContext ?? new OperationContext();

            return Executor.BeginExecuteAsync(
                                          QueryImpl(this, token, client, table, resolver, modifiedOptions),
                                          modifiedOptions.RetryPolicy,
                                          operationContext,
                                          callback,
                                          state);
        }

        internal TableQuerySegment<TResult> EndExecuteQuerySegmentedInternal<TResult>(IAsyncResult asyncResult)
        {
            return Executor.EndExecuteAsync<TableQuerySegment<TResult>>(asyncResult);
        }

        private static RESTCommand<TableQuerySegment<RESULT_TYPE>> QueryImpl<T, RESULT_TYPE>(TableQuery<T> query, TableContinuationToken token, CloudTableClient client, CloudTable table, EntityResolver<RESULT_TYPE> resolver, TableRequestOptions requestOptions)
        {
            requestOptions.AssertPolicyIfRequired();

            // If encryption policy is set, then add the encryption metadata column to Select columns in order to be able to decrypt properties.
            if (requestOptions.EncryptionPolicy != null && query.SelectColumns != null && query.SelectColumns.Count() > 0)
            {
                query.SelectColumns.Add(Constants.EncryptionConstants.TableEncryptionKeyDetails);
                query.SelectColumns.Add(Constants.EncryptionConstants.TableEncryptionPropertyDetails);
            }

            UriQueryBuilder builder = query.GenerateQueryBuilder(requestOptions.ProjectSystemProperties);

            if (token != null)
            {
                token.ApplyToUriQueryBuilder(builder);
            }

            StorageUri tempUri = NavigationHelper.AppendPathToUri(client.StorageUri, table.Name);
            RESTCommand<TableQuerySegment<RESULT_TYPE>> queryCmd = new RESTCommand<TableQuerySegment<RESULT_TYPE>>(client.Credentials, tempUri);
            requestOptions.ApplyToStorageCommand(queryCmd);

            queryCmd.CommandLocationMode = CommonUtility.GetListingLocationMode(token);
            queryCmd.RetrieveResponseStream = true;
            queryCmd.SignRequest = client.AuthenticationHandler.SignRequest;
            queryCmd.Builder = builder;
            queryCmd.ParseError = StorageExtendedErrorInformation.ReadFromStreamUsingODataLib;
            queryCmd.BuildRequestDelegate = (uri, queryBuilder, timeout, useVersionHeader, ctx) => TableOperationHttpWebRequestFactory.BuildRequestForTableQuery(uri, queryBuilder, timeout, useVersionHeader, ctx, requestOptions.PayloadFormat.Value);

            queryCmd.PreProcessResponse = (cmd, resp, ex, ctx) => HttpResponseParsers.ProcessExpectedStatusCodeNoException(HttpStatusCode.OK, resp != null ? resp.StatusCode : HttpStatusCode.Unused, null /* retVal */, cmd, ex);
            queryCmd.PostProcessResponse = (cmd, resp, ctx) =>
            {
                ResultSegment<RESULT_TYPE> resSeg = TableOperationHttpResponseParsers.TableQueryPostProcessGeneric<RESULT_TYPE, T>(cmd.ResponseStream, resolver.Invoke, resp, requestOptions, ctx, client.AccountName);
                if (resSeg.ContinuationToken != null)
                {
                    resSeg.ContinuationToken.TargetLocation = cmd.CurrentResult.TargetLocation;
                }

                return new TableQuerySegment<RESULT_TYPE>(resSeg);
            };

            return queryCmd;
        }

        #endregion
    }
}
