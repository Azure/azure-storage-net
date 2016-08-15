// -----------------------------------------------------------------------------------------
// <copyright file="TableQueryGeneric.Common.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Table.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Text;

    /// <summary>
    /// Represents a query against a Microsoft Azure table.
    /// </summary>
    public partial class TableQuery<TElement>
    {
        #region Properties

        private int? takeCount = null;

        /// <summary>
        /// Gets or sets the number of entities the query returns specified in the table query. 
        /// </summary>
        /// <value>The maximum number of entities for the table query to return.</value>
        public int? TakeCount
        {
            get
            {
                return this.takeCount;
            }

            set
            {
                if (value.HasValue && value.Value <= 0)
                {
                    throw new ArgumentException(SR.TakeCountNotPositive);
                }

                this.takeCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the filter expression to use in the table query.
        /// </summary>
        /// <value>A string containing the filter expression to use in the query.</value>
        public string FilterString { get; set; }

        /// <summary>
        /// Gets or sets the property names of the table entity properties to return when the table query is executed.
        /// </summary>
        /// <value>A list of strings containing the property names of the table entity properties to return when the query is executed.</value>
        public IList<string> SelectColumns { get; set; }

        /// <summary>
        /// Defines the property names of the table entity properties to return when the table query is executed. 
        /// </summary>
        /// <param name="columns">A list of string objects containing the property names of the table entity properties to return when the query is executed.</param>
        /// <returns>A <see cref="TableQuery"/> instance set with the table entity properties to return.</returns>
        /// <remarks>The select clause is optional on a table query, and is used to limit the table properties returned from the server. 
        /// By default, a query will return all properties from the table entity.</remarks>
        public TableQuery<TElement> Select(IList<string> columns)
        {
#if WINDOWS_DESKTOP 
            if (this.Expression != null)
            {
                throw new NotSupportedException(SR.TableQueryFluentMethodNotAllowed);
            }
#endif

            this.SelectColumns = columns;
            return this;
        }

        /// <summary>
        /// Defines the upper bound for the number of entities the query returns.
        /// </summary>
        /// <param name="take">The maximum number of entities for the table query to return.</param>
        /// <returns>A <see cref="TableQuery"/> instance set with the number of entities to return.</returns>
        [SuppressMessage("Microsoft.Naming", "CA1719:ParameterNamesShouldNotMatchMemberNames", MessageId = "0#", Justification = "Reviewed.")]
        public TableQuery<TElement> Take(int? take)
        {
#if WINDOWS_DESKTOP 
            if (this.Expression != null)
            {
                throw new NotSupportedException(SR.TableQueryFluentMethodNotAllowed);
            }
#endif

            this.TakeCount = take;
            return this;
        }

        /// <summary>
        /// Defines a filter expression for the table query. Only entities that satisfy the specified filter expression will be returned by the query. 
        /// </summary>
        /// <remarks>Setting a filter expression is optional; by default, all entities in the table are returned if no filter expression is specified in the table query.</remarks>
        /// <param name="filter">A string containing the filter expression to apply to the table query.</param>
        /// <returns>A <see cref="TableQuery"/> instance set with the filter on entities to return.</returns>
        public TableQuery<TElement> Where(string filter)
        {
#if WINDOWS_DESKTOP 
            if (this.Expression != null)
            {
                throw new NotSupportedException(SR.TableQueryFluentMethodNotAllowed);
            }
#endif

            this.FilterString = filter;
            return this;
        }
#endregion

#region Impl

        internal UriQueryBuilder GenerateQueryBuilder(bool? projectSystemProperties)
        {
            UriQueryBuilder builder = new UriQueryBuilder();

            // filter
            if (!string.IsNullOrEmpty(this.FilterString))
            {
                builder.Add(TableConstants.Filter, this.FilterString);
            }

            // take
            if (this.takeCount.HasValue)
            {
                builder.Add(TableConstants.Top, Convert.ToString(Math.Min(this.takeCount.Value, TableConstants.TableServiceMaxResults), CultureInfo.InvariantCulture));
            }

            // select
            if (this.SelectColumns != null && this.SelectColumns.Count > 0)
            {
                StringBuilder colBuilder = new StringBuilder();
                bool foundRk = false;
                bool foundPk = false;
                bool foundTs = false;

                for (int m = 0; m < this.SelectColumns.Count; m++)
                {
                    if (this.SelectColumns[m] == TableConstants.PartitionKey)
                    {
                        foundPk = true;
                    }
                    else if (this.SelectColumns[m] == TableConstants.RowKey)
                    {
                        foundRk = true;
                    }
                    else if (this.SelectColumns[m] == TableConstants.Timestamp)
                    {
                        foundTs = true;
                    }

                    colBuilder.Append(this.SelectColumns[m]);
                    if (m < this.SelectColumns.Count - 1)
                    {
                        colBuilder.Append(",");
                    }
                }

                if (projectSystemProperties.Value)
                {
                    if (!foundPk)
                    {
                        colBuilder.Append(",");
                        colBuilder.Append(TableConstants.PartitionKey);
                    }

                    if (!foundRk)
                    {
                        colBuilder.Append(",");
                        colBuilder.Append(TableConstants.RowKey);
                    }

                    if (!foundTs)
                    {
                        colBuilder.Append(",");
                        colBuilder.Append(TableConstants.Timestamp);
                    }
                }

                builder.Add(TableConstants.Select, colBuilder.ToString());
            }

            return builder;
        }
#endregion
    }
}
