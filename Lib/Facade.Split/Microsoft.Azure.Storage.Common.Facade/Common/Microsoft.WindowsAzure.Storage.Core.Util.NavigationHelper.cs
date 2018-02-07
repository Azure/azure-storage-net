using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Core.Auth;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace Microsoft.Azure.Storage.Core.Util
{
internal static class NavigationHelper
{
    public static readonly char[] SlashAsSplitOptions = "/".ToCharArray();
    public static readonly char[] DotAsSplitOptions = ".".ToCharArray();
    public const string RootContainerName = "$root";
    public const string Slash = "/";
    public const string Dot = ".";
    public const char SlashChar = '/';

    internal static string GetContainerName(Uri blobAddress, bool? usePathStyleUris)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetBlobName(Uri blobAddress, bool? usePathStyleUris)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetShareName(Uri fileAddress, bool? usePathStyleUris)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetFileName(Uri fileAddress, bool? usePathStyleUris)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetFileAndDirectoryName(Uri fileAddress, bool? usePathStyleUris)
    {
        throw new System.NotImplementedException();
    }
    internal static bool GetBlobParentNameAndAddress(StorageUri blobAddress, string delimiter, bool? usePathStyleUris, out string parentName, out StorageUri parentAddress)
    {
        throw new System.NotImplementedException();
    }
    internal static bool GetFileParentNameAndAddress(StorageUri fileAddress, bool? usePathStyleUris, out string parentName, out StorageUri parentAddress)
    {
        throw new System.NotImplementedException();
    }
    internal static StorageUri GetServiceClientBaseAddress(StorageUri addressUri, bool? usePathStyleUris)
    {
        throw new System.NotImplementedException();
    }
    internal static Uri GetServiceClientBaseAddress(Uri addressUri, bool? usePathStyleUris)
    {
        throw new System.NotImplementedException();
    }
    internal static StorageUri AppendPathToUri(StorageUri uriList, string relativeUri)
    {
        throw new System.NotImplementedException();
    }
    internal static StorageUri AppendPathToUri(StorageUri uriList, string relativeUri, string sep)
    {
        throw new System.NotImplementedException();
    }
    internal static Uri AppendPathToSingleUri(Uri uri, string relativeUri)
    {
        throw new System.NotImplementedException();
    }
    internal static Uri AppendPathToSingleUri(Uri uri, string relativeUri, string sep)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetContainerNameFromContainerAddress(Uri uri, bool? usePathStyleUris)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetQueueNameFromUri(Uri uri, bool? usePathStyleUris)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetTableNameFromUri(Uri uri, bool? usePathStyleUris)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetShareNameFromShareAddress(Uri uri, bool? usePathStyleUris)
    {
        throw new System.NotImplementedException();
    }
    private static bool GetContainerNameAndAddress(StorageUri blobAddress, bool? usePathStyleUris, out string containerName, out StorageUri containerUri)
    {
        throw new System.NotImplementedException();
    }
    private static void GetShareNameAndAddress(StorageUri fileAddress, bool? usePathStyleUris, out string shareName, out StorageUri shareUri)
    {
        throw new System.NotImplementedException();
    }
    private static bool GetContainerNameAndBlobName(Uri blobAddress, bool? usePathStyleUris, out string containerName, out string blobName)
    {
        throw new System.NotImplementedException();
    }
    private static void GetShareNameAndFileName(Uri fileAddress, bool? usePathStyleUris, out string shareName, out string fileName)
    {
        throw new System.NotImplementedException();
    }
    internal static DateTimeOffset ParseSnapshotTime(string snapshot)
    {
        throw new System.NotImplementedException();
    }
    internal static StorageUri ParseBlobQueryAndVerify(StorageUri address, out StorageCredentials parsedCredentials, out DateTimeOffset? parsedSnapshot)
    {
        throw new System.NotImplementedException();
    }
    private static Uri ParseBlobQueryAndVerify(Uri address, out StorageCredentials parsedCredentials, out DateTimeOffset? parsedSnapshot)
    {
        throw new System.NotImplementedException();
    }
    internal static StorageUri ParseFileQueryAndVerify(StorageUri address, out StorageCredentials parsedCredentials, out DateTimeOffset? parsedShareSnapshot)
    {
        throw new System.NotImplementedException();
    }
    private static Uri ParseFileQueryAndVerify(Uri address, out StorageCredentials parsedCredentials, out DateTimeOffset? parsedShareSnapshot)
    {
        throw new System.NotImplementedException();
    }
    internal static StorageUri ParseQueueTableQueryAndVerify(StorageUri address, out StorageCredentials parsedCredentials)
    {
        throw new System.NotImplementedException();
    }
    private static Uri ParseQueueTableQueryAndVerify(Uri address, out StorageCredentials parsedCredentials)
    {
        throw new System.NotImplementedException();
    }
    internal static string GetAccountNameFromUri(Uri clientUri, bool? usePathStyleUris)
    {
        throw new System.NotImplementedException();
    }
}

}