using System;
using System.Globalization;
using System.Text.RegularExpressions;
namespace Microsoft.Azure.Storage
{
public static class NameValidator
{
    private static readonly string[] ReservedFileNames = new string[25] { ".", "..", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "PRN", "AUX", "NUL", "CON", "CLOCK$" };
    private static readonly RegexOptions RegexOptions = RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture | RegexOptions.Singleline;
    private static readonly Regex FileDirectoryRegex = new Regex("^[^\"\\\\/:|<>*?]*\\/{0,1}$", NameValidator.RegexOptions);
    private static readonly Regex ShareContainerQueueRegex = new Regex("^[a-z0-9]+(-[a-z0-9]+)*$", NameValidator.RegexOptions);
    private static readonly Regex TableRegex = new Regex("^[A-Za-z][A-Za-z0-9]*$", NameValidator.RegexOptions);
    private static readonly Regex MetricsTableRegex = new Regex("^\\$Metrics(HourPrimary|MinutePrimary|HourSecondary|MinuteSecondary)?(Transactions)(Blob|Queue|Table)$", NameValidator.RegexOptions);
    private const int BlobFileDirectoryMinLength = 1;
    private const int ContainerShareQueueTableMinLength = 3;
    private const int ContainerShareQueueTableMaxLength = 63;
    private const int FileDirectoryMaxLength = 255;
    private const int BlobMaxLength = 1024;

    public static void ValidateContainerName(string containerName)
    {
        throw new System.NotImplementedException();
    }
    public static void ValidateQueueName(string queueName)
    {
        throw new System.NotImplementedException();
    }
    public static void ValidateShareName(string shareName)
    {
        throw new System.NotImplementedException();
    }
    private static void ValidateShareContainerQueueHelper(string resourceName, string resourceType)
    {
        throw new System.NotImplementedException();
    }
    public static void ValidateBlobName(string blobName)
    {
        throw new System.NotImplementedException();
    }
    public static void ValidateFileName(string fileName)
    {
        throw new System.NotImplementedException();
    }
    public static void ValidateDirectoryName(string directoryName)
    {
        throw new System.NotImplementedException();
    }
    private static void ValidateFileDirectoryHelper(string resourceName, string resourceType)
    {
        throw new System.NotImplementedException();
    }
    public static void ValidateTableName(string tableName)
    {
        throw new System.NotImplementedException();
    }
}

}