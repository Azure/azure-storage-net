// -----------------------------------------------------------------------------------------
// <copyright file="NameValidator.cs" company="Microsoft">
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
// ----------------------------------------------------------------------------------------

namespace Microsoft.WindowsAzure.Storage
{
    using Microsoft.WindowsAzure.Storage.Core;
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides helpers to validate resource names across the Microsoft Azure Storage Services.
    /// </summary>
    public static class NameValidator
    {
        private const int BlobFileDirectoryMinLength = 1;
        private const int ContainerShareQueueTableMinLength = 3;
        private const int ContainerShareQueueTableMaxLength = 63;
        private const int FileDirectoryMaxLength = 255;
        private const int BlobMaxLength = 1024;
        private static readonly string[] ReservedFileNames = { ".", "..", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "PRN", "AUX", "NUL", "CON", "CLOCK$" };
        private static readonly RegexOptions RegexOptions = RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.CultureInvariant;
        private static readonly Regex FileDirectoryRegex = new Regex(@"^[^""\\/:|<>*?]*\/{0,1}$", NameValidator.RegexOptions);
        private static readonly Regex ShareContainerQueueRegex = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$", NameValidator.RegexOptions);
        private static readonly Regex TableRegex = new Regex("^[A-Za-z][A-Za-z0-9]*$", NameValidator.RegexOptions);
        private static readonly Regex MetricsTableRegex = new Regex(@"^\$Metrics(HourPrimary|MinutePrimary|HourSecondary|MinuteSecondary)?(Transactions)(Blob|Queue|Table)$", NameValidator.RegexOptions);
        
        /// <summary>
        /// Checks if a container name is valid.
        /// </summary>
        /// <param name="containerName">A string representing the container name to validate.</param>
        public static void ValidateContainerName(string containerName)
        {
            if (!("$root".Equals(containerName, StringComparison.Ordinal) || "$logs".Equals(containerName, StringComparison.Ordinal)))
            {
                NameValidator.ValidateShareContainerQueueHelper(containerName, SR.Container);
            }
        }

        /// <summary>
        /// Checks if a queue name is valid.
        /// </summary>
        /// <param name="queueName">A string representing the queue name to validate.</param>
        public static void ValidateQueueName(string queueName)
        {
            NameValidator.ValidateShareContainerQueueHelper(queueName, SR.Queue);
        }

        /// <summary>
        /// Checks if a share name is valid.
        /// </summary>
        /// <param name="shareName">A string representing the share name to validate.</param>
        public static void ValidateShareName(string shareName)
        {
            NameValidator.ValidateShareContainerQueueHelper(shareName, SR.Share);
        }

        private static void ValidateShareContainerQueueHelper(string resourceName, string resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.ResourceNameEmpty, resourceType));
            }

            if (resourceName.Length < NameValidator.ContainerShareQueueTableMinLength || resourceName.Length > NameValidator.ContainerShareQueueTableMaxLength)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.InvalidResourceNameLength, resourceType, NameValidator.ContainerShareQueueTableMinLength, NameValidator.ContainerShareQueueTableMaxLength));
            }

            if (!NameValidator.ShareContainerQueueRegex.IsMatch(resourceName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.InvalidResourceName, resourceType));
            }
        }

        /// <summary>
        /// Checks if a blob name is valid.
        /// </summary>
        /// <param name="blobName">A string representing the blob name to validate.</param>
        public static void ValidateBlobName(string blobName)
        {
            if (string.IsNullOrWhiteSpace(blobName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.ResourceNameEmpty, SR.Blob));
            }

            if (blobName.Length < NameValidator.BlobFileDirectoryMinLength || blobName.Length > NameValidator.BlobMaxLength)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.InvalidResourceNameLength, SR.Blob, NameValidator.BlobFileDirectoryMinLength, NameValidator.BlobMaxLength));
            }

            int slashCount = 0;
            foreach (char c in blobName)
            {
                if (c == '/')
                {
                    slashCount++;
                }
            }

            // 254 slashes means 255 path segments; max 254 segments for blobs, 255 includes container. 
            if (slashCount >= 254)
            {
                throw new ArgumentException(SR.TooManyPathSegments);
            }
        }

        /// <summary>
        /// Checks if a file name is valid.
        /// </summary>
        /// <param name="fileName">A string representing the file name to validate.</param>
        public static void ValidateFileName(string fileName)
        {
            NameValidator.ValidateFileDirectoryHelper(fileName, SR.File);

            if (fileName.EndsWith("/", StringComparison.Ordinal))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.InvalidResourceName, SR.File));
            }

            foreach (string s in NameValidator.ReservedFileNames)
            {
                if (s.Equals(fileName, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.InvalidResourceReservedName, SR.File));
                }
            }
        }

        /// <summary>
        /// Checks if a directory name is valid.
        /// </summary>
        /// <param name="directoryName">A string representing the directory name to validate.</param>
        public static void ValidateDirectoryName(string directoryName)
        {
            NameValidator.ValidateFileDirectoryHelper(directoryName, SR.Directory);
        }

        private static void ValidateFileDirectoryHelper(string resourceName, string resourceType)
        {
            if (string.IsNullOrWhiteSpace(resourceName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.ResourceNameEmpty, resourceType));
            }

            if (resourceName.Length < NameValidator.BlobFileDirectoryMinLength || resourceName.Length > NameValidator.FileDirectoryMaxLength)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.InvalidResourceNameLength, resourceType, NameValidator.BlobFileDirectoryMinLength, NameValidator.FileDirectoryMaxLength));
            }

            if (!NameValidator.FileDirectoryRegex.IsMatch(resourceName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.InvalidResourceName, resourceType));
            } 
        }

        /// <summary>
        /// Checks if a table name is valid.
        /// </summary>
        /// <param name="tableName">A string representing the table name to validate.</param>
        public static void ValidateTableName(string tableName)
        {
            if (string.IsNullOrWhiteSpace(tableName))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.ResourceNameEmpty, SR.Table));
            }

            if (tableName.Length < NameValidator.ContainerShareQueueTableMinLength || tableName.Length > NameValidator.ContainerShareQueueTableMaxLength)
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.InvalidResourceNameLength, SR.Table, NameValidator.ContainerShareQueueTableMinLength, NameValidator.ContainerShareQueueTableMaxLength));
            }

            if (!(NameValidator.TableRegex.IsMatch(tableName) || NameValidator.MetricsTableRegex.IsMatch(tableName) || tableName.Equals("$MetricsCapacityBlob", StringComparison.OrdinalIgnoreCase)))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, SR.InvalidResourceName, SR.Table));
            }
        }
    }
}