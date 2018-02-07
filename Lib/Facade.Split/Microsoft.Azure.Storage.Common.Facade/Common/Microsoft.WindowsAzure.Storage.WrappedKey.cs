
namespace Microsoft.Azure.Storage
{
internal class WrappedKey
{
    public string KeyId
    {
        get; set;
    }

    public byte[] EncryptedKey
    {
        get; set;
    }

    public string Algorithm
    {
        get; set;
    }

    public WrappedKey(string keyId, byte[] encryptedKey, string algorithm)
    {
        throw new System.NotImplementedException();
    }
}

}