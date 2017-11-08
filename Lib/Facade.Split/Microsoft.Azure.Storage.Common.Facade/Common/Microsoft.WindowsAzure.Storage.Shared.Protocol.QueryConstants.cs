using System;
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{

public static class QueryConstants
{
    public const string Snapshot = "snapshot";
    public const string ShareSnapshot = "sharesnapshot";
    public const string SignedStart = "st";
    public const string SignedExpiry = "se";
    public const string SignedResource = "sr";
    public const string SignedResourceTypes = "srt";
    public const string SignedServices = "ss";
    public const string SignedProtocols = "spr";
    public const string SignedIP = "sip";
    public const string SasTableName = "tn";
    public const string SignedPermissions = "sp";
    public const string StartPartitionKey = "spk";
    public const string StartRowKey = "srk";
    public const string EndPartitionKey = "epk";
    public const string EndRowKey = "erk";
    public const string SignedIdentifier = "si";
    public const string SignedKey = "sk";
    public const string SignedVersion = "sv";
    public const string Signature = "sig";
    public const string CacheControl = "rscc";
    public const string ContentType = "rsct";
    public const string ContentEncoding = "rsce";
    public const string ContentLanguage = "rscl";
    public const string ContentDisposition = "rscd";
    public const string ApiVersion = "api-version";
    public const string MessageTimeToLive = "messagettl";
    public const string VisibilityTimeout = "visibilitytimeout";
    public const string NumOfMessages = "numofmessages";
    public const string PopReceipt = "popreceipt";
    public const string ResourceType = "restype";
    public const string Component = "comp";
    public const string CopyId = "copyid";
}

}