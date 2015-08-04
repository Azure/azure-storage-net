//-----------------------------------------------------------------------
// <copyright file="NavigationHelper.cs" company="Microsoft">
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

namespace Microsoft.WindowsAzure.Storage.Core.Util
{
    using Microsoft.WindowsAzure.Storage.Auth;
    using Microsoft.WindowsAzure.Storage.Core.Auth;
    using Microsoft.WindowsAzure.Storage.Shared.Protocol;
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;

    /// <summary>
    /// Contains methods for dealing with navigation.
    /// </summary>
    internal static class NavigationHelper
    {
        /// <summary>
        /// The name of the root container.
        /// </summary>
        public const string RootContainerName = "$root";

        /// <summary>
        /// Used in address parsing.
        /// </summary>
        public const string Slash = "/";

        /// <summary>
        /// Used in address parsing.
        /// </summary>
        public const string Dot = ".";

        /// <summary>
        /// Used in address parsing.
        /// </summary>
        public const char SlashChar = '/';

        /// <summary>
        /// Used to split string on slash.
        /// </summary>
        public static readonly char[] SlashAsSplitOptions = Slash.ToCharArray();

        /// <summary>
        /// Used to split hostname.
        /// </summary>
        public static readonly char[] DotAsSplitOptions = Dot.ToCharArray();

        /// <summary>
        /// Retrieves the container part of a storage Uri, or "$root" if the container is implicit.
        /// </summary>
        /// <param name="blobAddress">The blob address.</param>
        /// <param name="usePathStyleUris">If set to <c>true</c> use path style Uris.</param>
        /// <returns>Name of the container.</returns>
        /// <remarks>
        /// The trailing slash is always removed.
        /// <example>
        /// GetContainerName(new Uri("http://test.blob.core.windows.net/mycontainer/myfolder/myblob")) will return "mycontainer"
        /// GetContainerName(new Uri("http://test.blob.core.windows.net/mycontainer/")) will return "mycontainer"
        /// GetContainerName(new Uri("http://test.blob.core.windows.net/myblob")) will return "$root"
        /// GetContainerName(new Uri("http://test.blob.core.windows.net/")) will throw ArgumentException
        /// </example>
        /// </remarks>
        internal static string GetContainerName(Uri blobAddress, bool? usePathStyleUris)
        {
            string containerName;
            string blobName;

            GetContainerNameAndBlobName(blobAddress, usePathStyleUris, out containerName, out blobName);

            return containerName;
        }

        /// <summary>
        /// Retrieves the blob part of a storage Uri.
        /// </summary>
        /// <param name="blobAddress">The blob address.</param>
        /// <param name="usePathStyleUris">If set to <c>true</c> use path style Uris.</param>
        /// <returns>A string containing the name of the blob.</returns>
        internal static string GetBlobName(Uri blobAddress, bool? usePathStyleUris)
        {
            string containerName;
            string blobName;

            GetContainerNameAndBlobName(blobAddress, usePathStyleUris, out containerName, out blobName);

            return Uri.UnescapeDataString(blobName);
        }

        /// <summary>
        /// Retrieves the share part of a storage Uri, or "$root" if the share is implicit.
        /// </summary>
        /// <param name="fileAddress">The file address.</param>
        /// <param name="usePathStyleUris">If set to <c>true</c> use path style Uris.</param>
        /// <returns>Name of the share.</returns>
        /// <remarks>
        /// The trailing slash is always removed.
        /// <example>
        /// GetShareName(new Uri("http://test.file.core.windows.net/myshare/myfolder/myfile")) will return "myshare"
        /// GetShareName(new Uri("http://test.file.core.windows.net/myshare/")) will return "myshare"
        /// GetShareName(new Uri("http://test.file.core.windows.net/")) will throw ArgumentException
        /// </example>
        /// </remarks>
        internal static string GetShareName(Uri fileAddress, bool? usePathStyleUris)
        {
            string shareName;
            string fileName;

            GetShareNameAndFileName(fileAddress, usePathStyleUris, out shareName, out fileName);

            return shareName;
        }

        /// <summary>
        /// Retrieves the file part of a storage Uri.
        /// </summary>
        /// <param name="fileAddress">The file address.</param>
        /// <param name="usePathStyleUris">If set to <c>true</c> use path style Uris.</param>
        /// <returns>The name of the file.</returns>
        internal static string GetFileName(Uri fileAddress, bool? usePathStyleUris)
        {
            string shareName;
            string fileName;

            GetShareNameAndFileName(fileAddress, usePathStyleUris, out shareName, out fileName);

            return Uri.UnescapeDataString(fileName);
        }

        /// <summary>
        /// Retrieves the file and directory part of a storage Uri.
        /// </summary>
        /// <param name="fileAddress">The file address.</param>
        /// <param name="usePathStyleUris">If set to <c>true</c> use path style Uris.</param>
        /// <returns>The file name including directories.</returns>
        internal static string GetFileAndDirectoryName(Uri fileAddress, bool? usePathStyleUris)
        {
            CommonUtility.AssertNotNull("fileAddress", fileAddress);

            if (usePathStyleUris == null)
            {
                // Automatically determine whether to use path style vs host style uris
                usePathStyleUris = CommonUtility.UsePathStyleAddressing(fileAddress);
            }

            string[] addressParts = fileAddress.Segments;
            int shareIndex = usePathStyleUris.Value ? 2 : 1;

            if (addressParts.Length - 1 < shareIndex)
            {
                // No reference appears to any share or file.
                string error = string.Format(CultureInfo.CurrentCulture, SR.MissingShareInformation, fileAddress);
                throw new ArgumentException(error, "fileAddress");
            }
            else if (addressParts.Length - 1 == shareIndex)
            {
                // This is root directory of a share.
                return string.Empty;
            }
            else
            {
                // This is a file with directories (the relevant case).
                // Skip (shareIndex + 1) because Skip takes a count, not an index, as a param.
                return Uri.UnescapeDataString(string.Concat(addressParts.Skip(shareIndex + 1)));
            }
        }

        /// <summary>
        /// Retrieves the parent name from a storage Uri.
        /// </summary>
        /// <param name="blobAddress">The blob address.</param>
        /// <param name="delimiter">The delimiter.</param>
        /// <param name="usePathStyleUris">If set to <c>true</c> use path style Uris.</param>
        /// <param name="parentName">Name of the parent.</param>
        /// <param name="parentAddress">The parent URI.</param>
        /// <returns>The name of the parent.</returns>
        /// <remarks>
        /// Adds the trailing delimiter as the prefix returned by the storage REST api always contains the delimiter.
        /// </remarks>
        /// <example>
        /// GetBlobParentNameAndAddress(new Uri("http://test.blob.core.windows.net/mycontainer/myfolder/myblob", "/")) will return "/myfolder/"
        /// GetBlobParentNameAndAddress(new Uri("http://test.blob.core.windows.net/mycontainer/myfolder|myblob", "|") will return "/myfolder|"
        /// GetBlobParentNameAndAddress(new Uri("http://test.blob.core.windows.net/mycontainer/myblob", "/") will return ""
        /// GetBlobParentNameAndAddress(new Uri("http://test.blob.core.windows.net/mycontainer/", "/") will return ""
        /// </example>
        internal static bool GetBlobParentNameAndAddress(StorageUri blobAddress, string delimiter, bool? usePathStyleUris, out string parentName, out StorageUri parentAddress)
        {
            CommonUtility.AssertNotNull("blobAbsoluteUriString", blobAddress);
            CommonUtility.AssertNotNullOrEmpty("delimiter", delimiter);

            parentName = null;
            parentAddress = null;

            string containerName;
            StorageUri containerUri;
            if (!GetContainerNameAndAddress(blobAddress, usePathStyleUris, out containerName, out containerUri))
            {
                return false;
            }

            // Get the blob path as the rest of the Uri
            Uri blobPathUri = containerUri.PrimaryUri.MakeRelativeUri(blobAddress.PrimaryUri);
            string blobPath = blobPathUri.OriginalString;

            delimiter = Uri.EscapeUriString(delimiter);
            if (blobPath.EndsWith(delimiter, StringComparison.Ordinal))
            {
                blobPath = blobPath.Substring(0, blobPath.Length - delimiter.Length);
            }

            if (string.IsNullOrEmpty(blobPath))
            {
                // Case 1 /<ContainerName>[Delimiter]*? => /<ContainerName>
                // Parent of container is container itself
            }
            else
            {
                int parentLength = blobPath.LastIndexOf(delimiter, StringComparison.Ordinal);

                if (parentLength <= 0)
                {
                    // Case 2 /<Container>/<folder>
                    // Parent of a folder is container
                    parentName = string.Empty;
                    parentAddress = containerUri;
                }
                else
                {
                    // Case 3 /<Container>/<folder>/[<subfolder>/]*<BlobName>
                    // Parent of blob is folder
                    parentName = Uri.UnescapeDataString(blobPath.Substring(0, parentLength + delimiter.Length)).Substring(containerName.Length + 1);
                    parentAddress = NavigationHelper.AppendPathToUri(containerUri, parentName);
                }
            }

            return parentName != null;
        }

        /// <summary>
        /// Retrieves the parent name from a storage Uri.
        /// </summary>
        /// <param name="fileAddress">The file address.</param>
        /// <param name="usePathStyleUris">If set to <c>true</c> use path style Uris.</param>
        /// <param name="parentName">Name of the parent.</param>
        /// <param name="parentAddress">The parent URI.</param>
        /// <returns>The name of the parent.</returns>
        /// <remarks>
        /// Adds the trailing delimiter as the prefix returned by the storage REST api always contains the delimiter.
        /// </remarks>
        /// <example>
        /// GetFileParentNameAndAddress(new Uri("http://test.file.core.windows.net/myshare/myfolder/myfile", "/")) will return "myfolder"
        /// GetFileParentNameAndAddress(new Uri("http://test.file.core.windows.net/myshare/myfile", "/") will return ""
        /// GetFileParentNameAndAddress(new Uri("http://test.file.core.windows.net/myshare/", "/") will return ""
        /// </example>
        internal static bool GetFileParentNameAndAddress(StorageUri fileAddress, bool? usePathStyleUris, out string parentName, out StorageUri parentAddress)
        {
            CommonUtility.AssertNotNull("fileAbsoluteUriString", fileAddress);

            parentName = null;
            parentAddress = null;

            string shareName;
            StorageUri shareUri;
            GetShareNameAndAddress(fileAddress, usePathStyleUris, out shareName, out shareUri);

            // Get the file path as the rest of the Uri
            Uri filePathUri = shareUri.PrimaryUri.MakeRelativeUri(fileAddress.PrimaryUri);
            string filePath = filePathUri.OriginalString;

            if ((filePath.Length > 0) && (filePath[filePath.Length - 1] == NavigationHelper.SlashChar))
            {
                filePath = filePath.Substring(0, filePath.Length - 1);
            }

            if (string.IsNullOrEmpty(filePath))
            {
                // Case 1 /<ShareName>/*? => /<ShareName>
                // Parent of share is share itself
            }
            else
            {
                int parentLength = filePath.LastIndexOf(NavigationHelper.SlashChar);

                if (parentLength <= shareName.Length)
                {
                    // Case 2 /<Share>/<folder>
                    // Parent of a folder is share
                    parentName = string.Empty;
                    parentAddress = shareUri;
                }
                else
                {
                    // Case 3 /<Share>/<folder>/[<subfolder>/]*<FileName>
                    // Parent of file is folder
                    parentName = Uri.UnescapeDataString(filePath.Substring(0, parentLength)).Substring(shareName.Length + 1);
                    parentAddress = NavigationHelper.AppendPathToUri(shareUri, parentName);

                    parentLength = parentName.LastIndexOf(NavigationHelper.SlashChar);
                    if (parentLength >= 0)
                    {
                        parentName = parentName.Substring(parentLength + 1);
                    }
                }
            }

            return parentName != null;
        }

        /// <summary>
        /// Gets the service client base address.
        /// </summary>
        /// <param name="addressUri">The address Uri.</param>
        /// <param name="usePathStyleUris">The use path style Uris.</param>
        /// <returns>The base address of the client.</returns>
        /// <example>
        /// GetServiceClientBaseAddress("http://testaccount.blob.core.windows.net/testcontainer/blob1") 
        /// returns "http://testaccount.blob.core.windows.net"
        /// </example>
        internal static StorageUri GetServiceClientBaseAddress(StorageUri addressUri, bool? usePathStyleUris)
        {
            return new StorageUri(
                GetServiceClientBaseAddress(addressUri.PrimaryUri, usePathStyleUris),
                GetServiceClientBaseAddress(addressUri.SecondaryUri, usePathStyleUris));
        }

        /// <summary>
        /// Gets the service client base address.
        /// </summary>
        /// <param name="addressUri">The address Uri.</param>
        /// <param name="usePathStyleUris">The use path style Uris.</param>
        /// <returns>The base address of the client.</returns>
        /// <example>
        /// GetServiceClientBaseAddress("http://testaccount.blob.core.windows.net/testcontainer/blob1") 
        /// returns "http://testaccount.blob.core.windows.net"
        /// </example>
        internal static Uri GetServiceClientBaseAddress(Uri addressUri, bool? usePathStyleUris)
        {
            if (addressUri == null)
            {
                return null;
            }

            if (usePathStyleUris == null)
            {
                // Automatically determine whether to use path style vs host style uris
                usePathStyleUris = CommonUtility.UsePathStyleAddressing(addressUri);
            }

            Uri authority = new Uri(addressUri.GetComponents(UriComponents.SchemeAndServer, UriFormat.UriEscaped));
            if (usePathStyleUris.Value)
            {
                // Path style uri
                string[] segments = addressUri.Segments;
                if (segments.Length < 2)
                {
                    string error = string.Format(CultureInfo.CurrentCulture, SR.PathStyleUriMissingAccountNameInformation);
                    throw new ArgumentException("address", error);
                }

                return new Uri(authority, segments[1]);
            }
            else
            {
                return authority;
            }
        }

        /// <summary>
        /// Appends a path to a list of URIs correctly using "/" as separator.
        /// </summary>
        /// <param name="uriList">The base URI.</param>
        /// <param name="relativeUri">The relative or absolute URI.</param>
        /// <returns>The list of appended URIs.</returns>
        internal static StorageUri AppendPathToUri(StorageUri uriList, string relativeUri)
        {
            return AppendPathToUri(uriList, relativeUri, NavigationHelper.Slash);
        }

        /// <summary>
        /// Appends a path to a list of URIs correctly using "/" as separator.
        /// </summary>
        /// <param name="uriList">The base URI.</param>
        /// <param name="relativeUri">The relative or absolute URI.</param>
        /// <param name="sep">The separator.</param>
        /// <returns>The list of appended URIs.</returns>
        internal static StorageUri AppendPathToUri(StorageUri uriList, string relativeUri, string sep)
        {
            return new StorageUri(
                AppendPathToSingleUri(uriList.PrimaryUri, relativeUri, sep),
                AppendPathToSingleUri(uriList.SecondaryUri, relativeUri, sep));
        }

        /// <summary>
        /// Append a relative path to a URI, handling trailing slashes appropriately.
        /// </summary>
        /// <param name="uri">The base URI.</param>
        /// <param name="relativeUri">The relative or absolute URI.</param>
        /// <returns>The appended Uri.</returns>
        internal static Uri AppendPathToSingleUri(Uri uri, string relativeUri)
        {
            return AppendPathToSingleUri(uri, relativeUri, NavigationHelper.Slash);
        }

        /// <summary>
        /// Append a relative path to a URI, handling trailing slashes appropriately.
        /// </summary>
        /// <param name="uri">The base URI.</param>
        /// <param name="relativeUri">The relative or absolute URI.</param>
        /// <param name="sep">The separator.</param>
        /// <returns>The appended Uri.</returns>
        internal static Uri AppendPathToSingleUri(Uri uri, string relativeUri, string sep)
        {
            if (uri == null || relativeUri.Length == 0)
            {
                return uri;
            }

            sep = Uri.EscapeUriString(sep);
            relativeUri = Uri.EscapeUriString(relativeUri);

            UriBuilder ub = new UriBuilder(uri);
            string appendString = null;
            if (ub.Path.EndsWith(sep, StringComparison.Ordinal))
            {
                appendString = relativeUri;
            }
            else
            {
                appendString = sep + relativeUri;
            }

            ub.Path += appendString;
            return ub.Uri;
        }

        /// <summary>
        /// Get container name from address for styles of paths
        /// Example: http://test.blob.core.windows.net/container/blob =&gt; container
        /// http://127.0.0.1:10000/test/container/blob =&gt; container.
        /// </summary>
        /// <param name="uri">The container Uri.</param>
        /// <param name="usePathStyleUris">If set to <c>true</c> use path style Uris.</param>
        /// <returns>The container name.</returns>
        internal static string GetContainerNameFromContainerAddress(Uri uri, bool? usePathStyleUris)
        {
            if (usePathStyleUris == null)
            {
                // Automatically determine whether to use path style vs host style uris
                usePathStyleUris = CommonUtility.UsePathStyleAddressing(uri);
            }

            if (usePathStyleUris.Value)
            {
                string[] parts = uri.AbsolutePath.Split(NavigationHelper.SlashAsSplitOptions);

                if (parts.Length < 3)
                {
                    string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.MissingAccountInformationInUri, uri.AbsoluteUri);
                    throw new InvalidOperationException(errorMessage);
                }

                return parts[2];
            }
            else
            {
                return uri.AbsolutePath.Substring(1);
            }
        }

        /// <summary>
        /// Similar to getting container name from Uri.
        /// </summary>
        /// <param name="uri">The queue Uri.</param>
        /// <param name="usePathStyleUris">If set to <c>true</c> use path style Uris.</param>
        /// <returns>The queue name.</returns>
        internal static string GetQueueNameFromUri(Uri uri, bool? usePathStyleUris)
        {
            return GetContainerNameFromContainerAddress(uri, usePathStyleUris);
        }

        /// <summary>
        /// Extracts a table name from the table's Uri.
        /// </summary>
        /// <param name="uri">The queue Uri.</param>
        /// <param name="usePathStyleUris">If set to <c>true</c> use path style Uris.</param>
        /// <returns>The queue name.</returns>
        internal static string GetTableNameFromUri(Uri uri, bool? usePathStyleUris)
        {
            return GetContainerNameFromContainerAddress(uri, usePathStyleUris);
        }

        /// <summary>
        /// Extracts a table name from the share's Uri.
        /// </summary>
        /// <param name="uri">The share Uri.</param>
        /// <param name="usePathStyleUris">If set to <c>true</c> use path style Uris.</param>
        /// <returns>The share name.</returns>
        internal static string GetShareNameFromShareAddress(Uri uri, bool? usePathStyleUris)
        {
            return GetContainerNameFromContainerAddress(uri, usePathStyleUris);
        }

        /// <summary>
        /// Retrieve the container address and address.
        /// </summary>
        /// <param name="blobAddress">The blob address.</param>
        /// <param name="usePathStyleUris">True to use path style Uris.</param>
        /// <param name="containerName">Name of the container.</param>
        /// <param name="containerUri">The container URI.</param>
        /// <returns><c>true</c> when the container is an explicit container. <c>false</c>, otherwise.</returns> 
        private static bool GetContainerNameAndAddress(StorageUri blobAddress, bool? usePathStyleUris, out string containerName, out StorageUri containerUri)
        {
            string blobName;
            bool explicitCont = GetContainerNameAndBlobName(blobAddress.PrimaryUri, usePathStyleUris, out containerName, out blobName);
            containerUri = NavigationHelper.AppendPathToUri(GetServiceClientBaseAddress(blobAddress, usePathStyleUris), containerName);

            return explicitCont;
        }

        /// <summary>
        /// Retrieve the share address and address.
        /// </summary>
        /// <param name="fileAddress">The file address.</param>
        /// <param name="usePathStyleUris">True to use path style Uris.</param>
        /// <param name="shareName">Name of the share.</param>
        /// <param name="shareUri">The share URI.</param>
        /// <returns><c>true</c> when the share is an explicit share. <c>false</c>, otherwise.</returns> 
        private static void GetShareNameAndAddress(StorageUri fileAddress, bool? usePathStyleUris, out string shareName, out StorageUri shareUri)
        {
            string fileName;
            GetShareNameAndFileName(fileAddress.PrimaryUri, usePathStyleUris, out shareName, out fileName);
            shareUri = NavigationHelper.AppendPathToUri(GetServiceClientBaseAddress(fileAddress, usePathStyleUris), shareName);
        }

        /// <summary>
        /// Retrieve the container name and the blob name from a blob address.
        /// </summary>
        /// <param name="blobAddress">The blob address.</param>
        /// <param name="usePathStyleUris">If set to <c>true</c> use path style Uris.</param>
        /// <param name="containerName">The resulting container name.</param>
        /// <param name="blobName">The resulting blob name.</param>
        /// <returns>A bool representing whether the blob is in an explicit container or not.</returns>
        private static bool GetContainerNameAndBlobName(Uri blobAddress, bool? usePathStyleUris, out string containerName, out string blobName)
        {
            CommonUtility.AssertNotNull("blobAddress", blobAddress);

            if (usePathStyleUris == null)
            {
                // Automatically determine whether to use path style vs host style uris
                usePathStyleUris = CommonUtility.UsePathStyleAddressing(blobAddress);
            }

            string[] addressParts = blobAddress.Segments;

            int containerIndex = usePathStyleUris.Value ? 2 : 1;
            int firstBlobIndex = containerIndex + 1;

            if (addressParts.Length - 1 < containerIndex)
            {
                // No reference appears to any container or blob
                string error = string.Format(CultureInfo.CurrentCulture, SR.MissingContainerInformation, blobAddress);
                throw new ArgumentException(error, "blobAddress");
            }
            else if (addressParts.Length - 1 == containerIndex)
            {
                // This is either the root directory of a container or a blob in the root container
                string containerOrBlobName = addressParts[containerIndex];
                if (containerOrBlobName[containerOrBlobName.Length - 1] == SlashChar)
                {
                    // This is the root directory of a container
                    containerName = containerOrBlobName.Trim(SlashChar);
                    blobName = string.Empty;
                }
                else
                {
                    // This is a blob implicitly in the root container
                    containerName = RootContainerName;
                    blobName = containerOrBlobName;
                }
            }
            else
            {
                // This is a blob in an explicit container
                containerName = addressParts[containerIndex].Trim(SlashChar);
                string[] blobNameSegments = new string[addressParts.Length - firstBlobIndex];
                Array.Copy(addressParts, firstBlobIndex, blobNameSegments, 0, blobNameSegments.Length);
                blobName = string.Concat(blobNameSegments);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Retrieve the share name and the file or directory name from a file or directory address.
        /// </summary>
        /// <param name="fileAddress">The file address.</param>
        /// <param name="usePathStyleUris">If set to <c>true</c> use path style Uris.</param>
        /// <param name="shareName">The resulting share name.</param>
        /// <param name="fileName">The resulting file or directory name.</param>
        private static void GetShareNameAndFileName(Uri fileAddress, bool? usePathStyleUris, out string shareName, out string fileName)
        {
            CommonUtility.AssertNotNull("fileAddress", fileAddress);

            if (usePathStyleUris == null)
            {
                // Automatically determine whether to use path style vs host style uris
                usePathStyleUris = CommonUtility.UsePathStyleAddressing(fileAddress);
            }

            string[] addressParts = fileAddress.Segments;

            int shareIndex = usePathStyleUris.Value ? 2 : 1;

            if (addressParts.Length - 1 < shareIndex)
            {
                // No reference appears to any share or file
                string error = string.Format(CultureInfo.CurrentCulture, SR.MissingShareInformation, fileAddress);
                throw new ArgumentException(error, "fileAddress");
            }
            else if (addressParts.Length - 1 == shareIndex)
            {
                // This is the root directory of a share
                string shareOrFileName = addressParts[shareIndex];
                shareName = shareOrFileName.Trim(SlashChar);
                fileName = string.Empty;
            }
            else
            {
                // This is a file or a directory in a share
                shareName = addressParts[shareIndex].Trim(SlashChar);
                fileName = addressParts[addressParts.Length - 1];
            }
        }

        /// <summary>
        /// Parses the snapshot time.
        /// </summary>
        /// <param name="snapshot">The snapshot time.</param>
        /// <returns>The parsed snapshot time.</returns>
        internal static DateTimeOffset ParseSnapshotTime(string snapshot)
        {
            DateTimeOffset snapshotTime;

            if (!DateTimeOffset.TryParse(
                snapshot,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal,
                out snapshotTime))
            {
                CommonUtility.ArgumentOutOfRange("snapshot", snapshot);
            }

            return snapshotTime;
        }

        /// <summary>
        /// Parse Uri for SAS (Shared access signature) information.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="parsedCredentials">The credentials to use.</param>
        /// <param name="parsedSnapshot">The parsed snapshot.</param>
        /// <returns>The blob URI without credentials or snapshot info</returns>
        /// <exception cref="System.ArgumentException">address</exception>
        /// <remarks>
        /// Validate that no other query parameters are passed in.
        /// Any SAS information will be recorded as corresponding credentials instance.
        /// If credentials is passed in and it does not match the SAS information found, an
        /// exception will be thrown.
        /// Otherwise a new client is created based on SAS information or as anonymous credentials.
        /// </remarks>
        internal static StorageUri ParseBlobQueryAndVerify(StorageUri address, out StorageCredentials parsedCredentials, out DateTimeOffset? parsedSnapshot)
        {
            StorageCredentials secondaryCredentials;
            DateTimeOffset? secondarySnapshot;
            return new StorageUri(
                ParseBlobQueryAndVerify(address.PrimaryUri, out parsedCredentials, out parsedSnapshot),
                ParseBlobQueryAndVerify(address.SecondaryUri, out secondaryCredentials, out secondarySnapshot));
        }

        /// <summary>
        /// Parse Uri for SAS (Shared access signature) information.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="parsedCredentials">The credentials to use.</param>
        /// <param name="parsedSnapshot">The parsed snapshot.</param>
        /// <returns>The blob URI without credentials or snapshot info</returns>
        /// <exception cref="System.ArgumentException">address</exception>
        /// <remarks>
        /// Validate that no other query parameters are passed in.
        /// Any SAS information will be recorded as corresponding credentials instance.
        /// If credentials is passed in and it does not match the SAS information found, an
        /// exception will be thrown.
        /// Otherwise a new client is created based on SAS information or as anonymous credentials.
        /// </remarks>
        private static Uri ParseBlobQueryAndVerify(Uri address, out StorageCredentials parsedCredentials, out DateTimeOffset? parsedSnapshot)
        {
            parsedCredentials = null;
            parsedSnapshot = null;
            if (address == null)
            {
                return null;
            }

            if (!address.IsAbsoluteUri)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.RelativeAddressNotPermitted, address.ToString());
                throw new ArgumentException(errorMessage, "address");
            }

            IDictionary<string, string> queryParameters = HttpWebUtility.ParseQueryString(address.Query);

            string snapshot;
            if (queryParameters.TryGetValue(Constants.QueryConstants.Snapshot, out snapshot))
            {
                if (!string.IsNullOrEmpty(snapshot))
                {
                    parsedSnapshot = ParseSnapshotTime(snapshot);
                }
            }

            parsedCredentials = SharedAccessSignatureHelper.ParseQuery(queryParameters);

            // SAS credentials were passed in the Uri if parsedCredentials is non null.
            if (parsedCredentials != null)
            {
                string signedResource;
                queryParameters.TryGetValue(Constants.QueryConstants.SignedResource, out signedResource);

                if (string.IsNullOrEmpty(signedResource))
                {
                    string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.MissingMandatoryParametersForSAS);
                    throw new ArgumentException(errorMessage);
                }
            }

            return new Uri(address.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped));
        }

        /// <summary>
        /// Parse Uri for SAS (Shared access signature) information.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="parsedCredentials">The credentials to use.</param>
        /// <returns>The file URI without credentials info</returns>
        /// <exception cref="System.ArgumentException">address</exception>
        /// <remarks>
        /// Validate that no other query parameters are passed in.
        /// Any SAS information will be recorded as corresponding credentials instance.
        /// If credentials is passed in and it does not match the SAS information found, an
        /// exception will be thrown.
        /// Otherwise a new client is created based on SAS information or as anonymous credentials.
        /// </remarks>
        internal static StorageUri ParseFileQueryAndVerify(StorageUri address, out StorageCredentials parsedCredentials)
        {
            StorageCredentials secondaryCredentials;
            return new StorageUri(
                ParseFileQueryAndVerify(address.PrimaryUri, out parsedCredentials),
                ParseFileQueryAndVerify(address.SecondaryUri, out secondaryCredentials));
        }

        /// <summary>
        /// Parse Uri for SAS (Shared access signature) information.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="parsedCredentials">The credentials to use.</param>
        /// <returns>The file URI without credentials info</returns>
        /// <exception cref="System.ArgumentException">address</exception>
        /// <remarks>
        /// Validate that no other query parameters are passed in.
        /// Any SAS information will be recorded as corresponding credentials instance.
        /// If credentials is passed in and it does not match the SAS information found, an
        /// exception will be thrown.
        /// Otherwise a new client is created based on SAS information or as anonymous credentials.
        /// </remarks>
        private static Uri ParseFileQueryAndVerify(Uri address, out StorageCredentials parsedCredentials)
        {
            parsedCredentials = null;
            if (address == null)
            {
                return null;
            }

            if (!address.IsAbsoluteUri)
            {
                string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.RelativeAddressNotPermitted, address.ToString());
                throw new ArgumentException(errorMessage, "address");
            }

            IDictionary<string, string> queryParameters = HttpWebUtility.ParseQueryString(address.Query);

            parsedCredentials = SharedAccessSignatureHelper.ParseQuery(queryParameters);

            // SAS credentials were passed in the Uri if parsedCredentials is non null.
            if (parsedCredentials != null)
            {
                string signedResource;
                queryParameters.TryGetValue(Constants.QueryConstants.SignedResource, out signedResource);

                if (string.IsNullOrEmpty(signedResource))
                {
                    string errorMessage = string.Format(CultureInfo.CurrentCulture, SR.MissingMandatoryParametersForSAS);
                    throw new ArgumentException(errorMessage);
                }
            }

            return new Uri(address.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped));
        }

        /// <summary>
        /// Parse Uri for SAS (Shared access signature) information.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="parsedCredentials">The credentials to use.</param>
        /// <remarks>
        /// Validate that no other query parameters are passed in.
        /// Any SAS information will be recorded as corresponding credentials instance.
        /// If credentials is passed in and it does not match the SAS information found, an
        /// exception will be thrown.
        /// Otherwise a new client is created based on SAS information or as anonymous credentials.
        /// </remarks>
        internal static StorageUri ParseQueueTableQueryAndVerify(StorageUri address, out StorageCredentials parsedCredentials)
        {
            StorageCredentials secondaryCredentials;
            return new StorageUri(
                ParseQueueTableQueryAndVerify(address.PrimaryUri, out parsedCredentials),
                ParseQueueTableQueryAndVerify(address.SecondaryUri, out secondaryCredentials));
        }

        /// <summary>
        /// Parse Uri for SAS (Shared access signature) information.
        /// </summary>
        /// <param name="address">The complete Uri.</param>
        /// <param name="parsedCredentials">The credentials to use.</param>
        /// <remarks>
        /// Validate that no other query parameters are passed in.
        /// Any SAS information will be recorded as corresponding credentials instance.
        /// If credentials is passed in and it does not match the SAS information found, an
        /// exception will be thrown.
        /// Otherwise a new client is created based on SAS information or as anonymous credentials.
        /// </remarks>
        private static Uri ParseQueueTableQueryAndVerify(Uri address, out StorageCredentials parsedCredentials)
        {
            parsedCredentials = null;
            if (address == null)
            {
                return null;
            }

            if (!address.IsAbsoluteUri)
            {
                string errorMessage = string.Format(CultureInfo.InvariantCulture, SR.RelativeAddressNotPermitted, address.ToString());
                throw new ArgumentException(errorMessage, "address");
            }

            IDictionary<string, string> queryParameters = HttpWebUtility.ParseQueryString(address.Query);

            parsedCredentials = SharedAccessSignatureHelper.ParseQuery(queryParameters);
            return new Uri(address.GetComponents(UriComponents.SchemeAndServer | UriComponents.Path, UriFormat.UriEscaped));
        }

        internal static string GetAccountNameFromUri(Uri clientUri, bool? usePathStyleUris)
        {
            if (!usePathStyleUris.HasValue)
            {
                // Automatically determine whether to use path style vs host style uris
                usePathStyleUris = CommonUtility.UsePathStyleAddressing(clientUri);
            }

            string[] parts = clientUri.AbsoluteUri.Split(NavigationHelper.SlashAsSplitOptions, StringSplitOptions.RemoveEmptyEntries);

            if (usePathStyleUris.Value)
            {
                if (parts.Length < 3)
                {
                    string errorMessage = string.Format(CultureInfo.InvariantCulture, SR.MissingAccountInformationInUri, clientUri.AbsoluteUri);
                    throw new InvalidOperationException(errorMessage);
                }

                return parts[2];
            }
            else
            {
                if (parts.Length < 2)
                {
                    string errorMessage = string.Format(CultureInfo.InvariantCulture, SR.MissingAccountInformationInUri, clientUri.AbsoluteUri);
                    throw new InvalidOperationException(errorMessage);
                }

                int endIndex = parts[1].IndexOf(".", StringComparison.Ordinal);
                return parts[1].Substring(0, endIndex);
            }
        }
    }
}
