using System;
namespace Microsoft.WindowsAzure.Storage.Shared.Protocol
{

public static class EncryptionConstants
{
    internal const string EncryptionProtocolV1 = "1.0";
    internal const string KeyWrappingIV = "KeyWrappingIV";
    public const string BlobEncryptionData = "encryptiondata";
    public const string TableEncryptionKeyDetails = "_ClientEncryptionMetadata1";
    public const string TableEncryptionPropertyDetails = "_ClientEncryptionMetadata2";
    public const string AgentMetadataKey = "EncryptionLibrary";
    public const string AgentMetadataValue = ".NET 9.3.1";
}

}