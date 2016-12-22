using System.Collections.Generic;
namespace Microsoft.WindowsAzure.Storage
{
internal class EncryptionData
{
    public WrappedKey WrappedContentKey
    {
        get; set;
    }

    public EncryptionAgent EncryptionAgent
    {
        get; set;
    }

    public byte[] ContentEncryptionIV
    {
        get; set;
    }

    public IDictionary<string, string> KeyWrappingMetadata
    {
        get; set;
    }
}

}