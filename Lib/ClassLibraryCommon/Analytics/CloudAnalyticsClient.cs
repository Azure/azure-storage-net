//-----------------------------------------------------------------------
// <copyright file="CloudAnalyticsClient.cs" company="Microsoft">
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
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Blob;
    using Microsoft.WindowsAzure.Storage.Core;
    using Microsoft.WindowsAzure.Storage.Core.Util;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using Microsoft.WindowsAzure.Storage.Table;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Text;

    /// <summary>
    /// Provides a client-side logical representation for Microsoft Azure Storage Analytics. 
    /// This client is used to configure and execute requests against storage analytics.
    /// </summary>
    /// <remarks>The analytics service client encapsulates the endpoints for the Blob and Table services. It also encapsulates 
    /// credentials for accessing the storage account.</remarks>
    public sealed class CloudAnalyticsClient
    {
        private CloudBlobClient blobClient;

        private CloudTableClient tableClient;

        internal string LogContainer { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloudAnalyticsClient"/> class using the specified Blob and Table service endpoints
        /// and account credentials.
        /// </summary>
        /// <param name="blobStorageUri">A <see cref="StorageUri"/> object containing the Blob service endpoint to use to create the client.</param>
        /// <param name="tableStorageUri">A <see cref="StorageUri"/> object containing the Table service endpoint to use to create the client.</param>
        /// <param name="credentials">A <see cref="StorageCredentials"/> object.</param>
        public CloudAnalyticsClient(StorageUri blobStorageUri, StorageUri tableStorageUri, StorageCredentials credentials)
        {
            CommonUtility.AssertNotNull("blobStorageUri", blobStorageUri);
            CommonUtility.AssertNotNull("tableStorageUri", tableStorageUri);

            this.blobClient = new CloudBlobClient(blobStorageUri, credentials);
            this.tableClient = new CloudTableClient(tableStorageUri, credentials);
            this.LogContainer = Constants.AnalyticsConstants.LogsContainer;
        }

        /// <summary>
        /// Gets a <see cref="CloudBlobDirectory"/> object containing the logs for the specified storage service.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <returns>A <see cref="CloudBlobDirectory"/> object.</returns>
        public CloudBlobDirectory GetLogDirectory(StorageService service)
        {
            return this.blobClient.GetContainerReference(this.LogContainer).GetDirectoryReference(service.ToString().ToLowerInvariant());
        }

        /// <summary>
        /// Gets the hourly metrics table for the specified storage service.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <returns>A <see cref="CloudTable"/> object.</returns>
        public CloudTable GetHourMetricsTable(StorageService service)
        {
            return this.GetHourMetricsTable(service, StorageLocation.Primary);
        }

        /// <summary>
        /// Gets the hourly metrics table for the specified storage service.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <param name="location">A <see cref="StorageLocation"/> enumeration value.</param>
        /// <returns>A <see cref="CloudTable"/> object.</returns>
        public CloudTable GetHourMetricsTable(StorageService service, StorageLocation location)
        {
            switch (service)
            {
                case StorageService.Blob:
                    if (location == StorageLocation.Primary)
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsHourPrimaryTransactionsBlob);
                    }
                    else
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsHourSecondaryTransactionsBlob);
                    }

                case StorageService.Queue:
                    if (location == StorageLocation.Primary)
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsHourPrimaryTransactionsQueue);
                    }
                    else
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsHourSecondaryTransactionsQueue);
                    }

                case StorageService.Table:
                    if (location == StorageLocation.Primary)
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsHourPrimaryTransactionsTable);
                    }
                    else
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsHourSecondaryTransactionsTable);
                    }

                case StorageService.File:
                    if (location == StorageLocation.Primary)
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsHourPrimaryTransactionsFile);
                    }
                    else
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsHourSecondaryTransactionsFile);
                    }

                default:
                    throw new ArgumentException(SR.InvalidStorageService);
            }
        }

        /// <summary>
        /// Gets the minute metrics table for the specified storage service.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <returns>A <see cref="CloudTable"/> object.</returns>
        public CloudTable GetMinuteMetricsTable(StorageService service)
        {
            return this.GetMinuteMetricsTable(service, StorageLocation.Primary);
        }

        /// <summary>
        /// Gets the minute metrics table for the specified storage service.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <param name="location">A <see cref="StorageLocation"/> enumeration value.</param>
        /// <returns>A <see cref="CloudTable"/> object.</returns>
        public CloudTable GetMinuteMetricsTable(StorageService service, StorageLocation location)
        {
            switch (service)
            {
                case StorageService.Blob:
                    if (location == StorageLocation.Primary)
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsMinutePrimaryTransactionsBlob);
                    }
                    else
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsMinuteSecondaryTransactionsBlob);
                    }

                case StorageService.Queue:
                    if (location == StorageLocation.Primary)
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsMinutePrimaryTransactionsQueue);
                    }
                    else
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsMinuteSecondaryTransactionsQueue);
                    }

                case StorageService.Table:
                    if (location == StorageLocation.Primary)
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsMinutePrimaryTransactionsTable);
                    }
                    else
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsMinuteSecondaryTransactionsTable);
                    }

                case StorageService.File:
                    if (location == StorageLocation.Primary)
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsMinutePrimaryTransactionsFile);
                    }
                    else
                    {
                        return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsMinuteSecondaryTransactionsFile);
                    }

                default:
                    throw new ArgumentException(SR.InvalidStorageService);
            }
        }

        /// <summary>
        /// Gets the capacity metrics table for the Blob service.
        /// </summary>
        /// <returns>A <see cref="CloudTable"/> object.</returns>
        public CloudTable GetCapacityTable()
        {
            return this.tableClient.GetTableReference(Constants.AnalyticsConstants.MetricsCapacityBlob);
        }

#if SYNC
        /// <summary>
        /// Returns an enumerable collection of log blobs containing Analytics log records. The blobs are retrieved lazily.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="ICloudBlob"/> and are retrieved lazily.</returns>
        public IEnumerable<ICloudBlob> ListLogs(StorageService service)
        {
            return this.ListLogs(service, LoggingOperations.All, BlobListingDetails.None, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Returns an enumerable collection of log blobs containing Analytics log records. The blobs are retrieved lazily.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <param name="operations">A <see cref="LoggingOperations"/> enumeration value that indicates the types of logging operations on which to filter the log blobs.</param>
        /// <param name="details">A <see cref="BlobListingDetails"/> enumeration value that indicates whether or not blob metadata should be returned. Only <c>None</c> and <c>Metadata</c> are valid values. </param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="ICloudBlob"/> and are retrieved lazily.</returns>
        /// <remarks>Note that specifying a logging operation type for the <paramref name="operations"/> parameter will return any Analytics log blob that contains the specified logging operation,
        /// even if that log blob also includes other types of logging operations. Also note that the only currently supported values for the <paramref name="details"/> 
        /// parameter are <c>None</c> and <c>Metadata</c>.</remarks>
        public IEnumerable<ICloudBlob> ListLogs(StorageService service, LoggingOperations operations, BlobListingDetails details, BlobRequestOptions options, OperationContext operationContext)
        {
            BlobListingDetails metadataDetails = BlobListingDetails.None;

            // Currently only support the ability to retrieve metadata on logs.
            if (details.HasFlag(BlobListingDetails.Copy) || details.HasFlag(BlobListingDetails.Snapshots) || details.HasFlag(BlobListingDetails.UncommittedBlobs))
            {
                throw new ArgumentException(SR.InvalidListingDetails);
            }

            // At least one LogType must be specified.
            if (operations == LoggingOperations.None)
            {
                throw new ArgumentException(SR.InvalidLoggingLevel);
            }

            if (details.HasFlag(BlobListingDetails.Metadata) || !operations.HasFlag(LoggingOperations.All))
            {
                metadataDetails = BlobListingDetails.Metadata;
            }

            IEnumerable<IListBlobItem> logs = this.GetLogDirectory(service).ListBlobs(true, metadataDetails, options, operationContext);
            return logs.Select(log => (ICloudBlob)log).Where(log => IsCorrectLogType(log, operations));
        }

        /// <summary>
        /// Returns an enumerable collection of log blobs containing Analytics log records. The blobs are retrieved lazily.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <param name="startTime">A <see cref="DateTimeOffset"/> object representing the start time for which logs should be retrieved.</param>
        /// <param name="endTime">A <see cref="DateTimeOffset"/> object representing the end time for which logs should be retrieved.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="ICloudBlob"/> and are retrieved lazily.</returns>
        public IEnumerable<ICloudBlob> ListLogs(StorageService service, DateTimeOffset startTime, DateTimeOffset? endTime)
        {
            return this.ListLogs(service, startTime, endTime, LoggingOperations.All, BlobListingDetails.None, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Returns an enumerable collection of log blobs containing Analytics log records. The blobs are retrieved lazily.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <param name="startTime">A <see cref="DateTimeOffset"/> object representing the start of the time range for which logs should be retrieved.</param>
        /// <param name="endTime">A <see cref="DateTimeOffset"/> object representing the end of the time range for which logs should be retrieved.</param>
        /// <param name="operations">A <see cref="LoggingOperations"/> enumeration value that indicates the types of logging operations on which to filter the log blobs.</param>
        /// <param name="details">A <see cref="BlobListingDetails"/> enumeration value that indicates whether or not blob metadata should be returned. Only <c>None</c> and <c>Metadata</c> are valid values. </param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="ICloudBlob"/> and are retrieved lazily.</returns>
        /// <remarks>Note that specifying a logging operation type for the <paramref name="operations"/> parameter will return any Analytics log blob that contains the specified logging operation,
        /// even if that log blob also includes other types of logging operations. Also note that the only currently supported values for the <paramref name="details"/> 
        /// parameter are <c>None</c> and <c>Metadata</c>.</remarks>
        public IEnumerable<ICloudBlob> ListLogs(StorageService service, DateTimeOffset startTime, DateTimeOffset? endTime, LoggingOperations operations, BlobListingDetails details, BlobRequestOptions options, OperationContext operationContext)
        {
            CloudBlobDirectory logDirectory = this.GetLogDirectory(service);
            BlobListingDetails metadataDetails = details;
            DateTimeOffset utcStartTime = startTime.ToUniversalTime();
            DateTimeOffset dateCounter = new DateTimeOffset(utcStartTime.Ticks - (utcStartTime.Ticks % TimeSpan.TicksPerHour), utcStartTime.Offset);
            DateTimeOffset? utcEndTime = null;
            string endPrefix = null;

            // Ensure that the date range is correct.
            if (endTime.HasValue)
            {
                utcEndTime = endTime.Value.ToUniversalTime();
                endPrefix = logDirectory.Prefix + utcEndTime.Value.ToString("yyyy/MM/dd/HH", CultureInfo.InvariantCulture);
                if (utcStartTime > utcEndTime.Value)
                {
                    string errorString = string.Format(CultureInfo.InvariantCulture, SR.StartTimeExceedsEndTime, startTime, endTime.Value);
                    throw new ArgumentException(errorString);
                }
            }

            // Currently only support the ability to retrieve metadata on logs.
            if (details.HasFlag(BlobListingDetails.Copy) || details.HasFlag(BlobListingDetails.Snapshots) || details.HasFlag(BlobListingDetails.UncommittedBlobs))
            {
                throw new ArgumentException(SR.InvalidListingDetails);
            }

            // At least one LogType must be specified.
            if (operations == LoggingOperations.None)
            {
                throw new ArgumentException(SR.InvalidLoggingLevel);
            }

            // If metadata or a specific LogType is specified, metadata should be retrieved.
            if (details.HasFlag(BlobListingDetails.Metadata) || !operations.HasFlag(LoggingOperations.All))
            {
                metadataDetails = BlobListingDetails.Metadata;
            }

            // Check logs using an hour-based prefix until we reach a day boundary.
            while (dateCounter.Hour > 0)
            {
                string currentPrefix = logDirectory.Prefix + dateCounter.ToString("yyyy/MM/dd/HH", CultureInfo.InvariantCulture);
                IEnumerable<IListBlobItem> currentLogs = logDirectory.Container.ListBlobs(currentPrefix, true, metadataDetails, options, operationContext);

                foreach (ICloudBlob log in currentLogs)
                {
                    if (!utcEndTime.HasValue || string.Compare(log.Parent.Prefix, endPrefix) <= 0)
                    {
                        if (IsCorrectLogType(log, operations))
                        {
                            yield return log;
                        }
                    }
                    else
                    {
                        yield break;
                    }
                }

                dateCounter = dateCounter.AddHours(1);
                if (dateCounter > DateTimeOffset.UtcNow.AddHours(1))
                {
                    yield break;
                }
            }

            // Check logs using a day-based prefix until we reach a month boundary.
            while (dateCounter.Day > 1)
            {
                string currentPrefix = logDirectory.Prefix + dateCounter.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
                IEnumerable<IListBlobItem> currentLogs = logDirectory.Container.ListBlobs(currentPrefix, true, metadataDetails, options, operationContext);

                foreach (ICloudBlob log in currentLogs)
                {
                    if (!utcEndTime.HasValue || string.Compare(log.Parent.Prefix, endPrefix) <= 0)
                    {
                        if (IsCorrectLogType(log, operations))
                        {
                            yield return log;
                        }
                    }
                    else
                    {
                        yield break;
                    }
                }

                dateCounter = dateCounter.AddDays(1);
                if (dateCounter > DateTimeOffset.UtcNow.AddHours(1))
                {
                    yield break;
                }
            }

            // Check logs using a month-based prefix until we reach a year boundary.
            while (dateCounter.Month > 1)
            {
                string currentPrefix = logDirectory.Prefix + dateCounter.ToString("yyyy/MM", CultureInfo.InvariantCulture);
                IEnumerable<IListBlobItem> currentLogs = logDirectory.Container.ListBlobs(currentPrefix, true, metadataDetails, options, operationContext);

                foreach (ICloudBlob log in currentLogs)
                {
                    if (!utcEndTime.HasValue || string.Compare(log.Parent.Prefix, endPrefix) <= 0)
                    {
                        if (IsCorrectLogType(log, operations))
                        {
                            yield return log;
                        }
                    }
                    else
                    {
                        yield break;
                    }
                }

                dateCounter = dateCounter.AddMonths(1);
                if (dateCounter > DateTimeOffset.UtcNow.AddHours(1))
                {
                    yield break;
                }
            }

            // Continue using a year-based prefix. 
            while (true)
            {
                string currentPrefix = logDirectory.Prefix + dateCounter.ToString("yyyy", CultureInfo.InvariantCulture);
                IEnumerable<IListBlobItem> currentLogs = logDirectory.Container.ListBlobs(currentPrefix, true, metadataDetails, options, operationContext);

                foreach (ICloudBlob log in currentLogs)
                {
                    if (!utcEndTime.HasValue || string.Compare(log.Parent.Prefix, endPrefix) <= 0)
                    {
                        if (IsCorrectLogType(log, operations))
                        {
                            yield return log;
                        }
                    }
                    else
                    {
                        yield break;
                    }
                }

                dateCounter = dateCounter.AddYears(1);
                if (dateCounter > DateTimeOffset.UtcNow.AddHours(1))
                { 
                    yield break;
                }
            }
        }

        /// <summary>
        /// Returns an enumerable collection of Analytics log records, retrieved lazily.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="LogRecord"/> and are retrieved lazily.</returns>
        public IEnumerable<LogRecord> ListLogRecords(StorageService service)
        {
            return this.ListLogRecords(service, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Returns an enumerable collection of Analytics log records, retrieved lazily.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="LogRecord"/> and are retrieved lazily.</returns>
        public IEnumerable<LogRecord> ListLogRecords(StorageService service, BlobRequestOptions options, OperationContext operationContext)
        {
            return CloudAnalyticsClient.ParseLogBlobs(this.ListLogs(service, LoggingOperations.All, BlobListingDetails.None, options, operationContext));
        }

        /// <summary>
        /// Returns an enumerable collection of Analytics log records, retrieved lazily.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <param name="startTime">A <see cref="DateTimeOffset"/> object representing the start of the time range for which logs should be retrieved.</param>
        /// <param name="endTime">A <see cref="DateTimeOffset"/> object representing the end of the time range for which logs should be retrieved.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="LogRecord"/> and are retrieved lazily.</returns>
        public IEnumerable<LogRecord> ListLogRecords(StorageService service, DateTimeOffset startTime, DateTimeOffset? endTime)
        {
            return this.ListLogRecords(service, startTime, endTime, null /* options */, null /* operationContext */);
        }

        /// <summary>
        /// Returns an enumerable collection of Analytics log records, retrieved lazily.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <param name="startTime">A <see cref="DateTimeOffset"/> object representing the start of the time range for which logs should be retrieved.</param>
        /// <param name="endTime">A <see cref="DateTimeOffset"/> object representing the end of the time range for which logs should be retrieved.</param>
        /// <param name="options">A <see cref="BlobRequestOptions"/> object that specifies additional options for the request.</param>
        /// <param name="operationContext">An <see cref="OperationContext"/> object that represents the context for the current operation.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="LogRecord"/> and are retrieved lazily.</returns>
        public IEnumerable<LogRecord> ListLogRecords(StorageService service, DateTimeOffset startTime, DateTimeOffset? endTime, BlobRequestOptions options, OperationContext operationContext)
        {
            return CloudAnalyticsClient.ParseLogBlobs(this.ListLogs(service, startTime, endTime, LoggingOperations.All, BlobListingDetails.None, options, operationContext));
        }

        /// <summary>
        /// Returns an enumerable collection of Analytics log records, retrieved lazily.
        /// </summary>
        /// <param name="logBlobs">An enumerable collection of <see cref="ICloudBlob"/> objects from which to parse log records.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="LogRecord"/> and are retrieved lazily.</returns>
        public static IEnumerable<LogRecord> ParseLogBlobs(IEnumerable<ICloudBlob> logBlobs)
        {
            return logBlobs.SelectMany(CloudAnalyticsClient.ParseLogBlob);
        }

        /// <summary>
        /// Returns an enumerable collection of Analytics log records, retrieved lazily.
        /// </summary>
        /// <param name="logBlob">The <see cref="ICloudBlob"/> object from which to parse log records.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="LogRecord"/> and are retrieved lazily.</returns>
        public static IEnumerable<LogRecord> ParseLogBlob(ICloudBlob logBlob)
        {
            using (Stream stream = ((CloudBlockBlob)logBlob).OpenRead())
            {
                using (LogRecordStreamReader reader = new LogRecordStreamReader(stream, (int)stream.Length))
                {
                    LogRecord log;
                    while (!reader.IsEndOfFile)
                    {
                        log = new LogRecord(reader);
                        yield return log;
                    }
                }
            }
        }
        
        /// <summary>
        /// Returns an enumerable collection of Analytics log records, retrieved lazily.
        /// </summary>
        /// <param name="stream">The <see cref="System.IO.Stream"/> object from which to parse log records.</param>
        /// <returns>An enumerable collection of objects that implement <see cref="LogRecord"/> and are retrieved lazily.</returns>
        public static IEnumerable<LogRecord> ParseLogStream(Stream stream)
        {
            LogRecordStreamReader reader = new LogRecordStreamReader(stream, (int)stream.Length);
            LogRecord log;
            while (!reader.IsEndOfFile)
            {
                log = new LogRecord(reader);
                yield return log;
            }
        }
#endif

        /// <summary>
        /// Creates a <see cref="TableQuery"/> object for querying the Blob service capacity table.
        /// </summary>
        /// <returns>A <see cref="TableQuery"/> object.</returns>
        /// <remarks>This method is applicable only to Blob service.</remarks>
        public TableQuery<CapacityEntity> CreateCapacityQuery()
        {
            CloudTable capacityTable = this.GetCapacityTable();
            return capacityTable.CreateQuery<CapacityEntity>();
        }

        /// <summary>
        /// Creates a <see cref="TableQuery"/> object for querying an hourly metrics log table.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <param name="location">A <see cref="StorageLocation"/> enumeration value.</param>
        /// <returns>A <see cref="TableQuery"/> object.</returns>
        public TableQuery<MetricsEntity> CreateHourMetricsQuery(StorageService service, StorageLocation location)
        {
            CloudTable hourMetricsTable = this.GetHourMetricsTable(service, location);
            return hourMetricsTable.CreateQuery<MetricsEntity>();
        }

        /// <summary>
        /// Creates a <see cref="TableQuery"/> object for querying a minute metrics log table.
        /// </summary>
        /// <param name="service">A <see cref="StorageService"/> enumeration value.</param>
        /// <param name="location">A <see cref="StorageLocation"/> enumeration value.</param>
        /// <returns>A <see cref="TableQuery"/> object.</returns>
        public TableQuery<MetricsEntity> CreateMinuteMetricsQuery(StorageService service, StorageLocation location)
        {
            CloudTable minuteMetricsTable = this.GetMinuteMetricsTable(service, location);
            return minuteMetricsTable.CreateQuery<MetricsEntity>();
        }

        internal static bool IsCorrectLogType(ICloudBlob logBlob, LoggingOperations operations)
        {
            IDictionary<string, string> metadata = logBlob.Metadata;
            string logTypeValue;

            bool hasLogtype = metadata.TryGetValue("LogType", out logTypeValue);

            if (!hasLogtype)
            {
                return true;
            }

            if (operations.HasFlag(LoggingOperations.Read) && logTypeValue.Contains("read"))
            {
                return true;
            }

            if (operations.HasFlag(LoggingOperations.Write) && logTypeValue.Contains("write"))
            {
                return true;
            }

            if (operations.HasFlag(LoggingOperations.Delete) && logTypeValue.Contains("delete"))
            {
                return true;
            }

            return false;
        }
    }
}