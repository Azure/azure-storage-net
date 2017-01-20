using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Core.Auth;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.File;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
namespace Microsoft.WindowsAzure.Storage
{
public sealed class CloudStorageAccount
{
    private static readonly KeyValuePair<string, Func<string, bool>> UseDevelopmentStorageSetting = CloudStorageAccount.Setting("UseDevelopmentStorage", new string[1] { "true" });
    private static readonly KeyValuePair<string, Func<string, bool>> DevelopmentStorageProxyUriSetting = CloudStorageAccount.Setting("DevelopmentStorageProxyUri", new Func<string, bool>(CloudStorageAccount.IsValidUri));
    private static readonly KeyValuePair<string, Func<string, bool>> DefaultEndpointsProtocolSetting = CloudStorageAccount.Setting("DefaultEndpointsProtocol", "http", "https");
    private static readonly KeyValuePair<string, Func<string, bool>> AccountNameSetting = CloudStorageAccount.Setting("AccountName");
    private static readonly KeyValuePair<string, Func<string, bool>> AccountKeyNameSetting = CloudStorageAccount.Setting("AccountKeyName");
    private static readonly KeyValuePair<string, Func<string, bool>> AccountKeySetting = CloudStorageAccount.Setting("AccountKey", new Func<string, bool>(CloudStorageAccount.IsValidBase64String));
    private static readonly KeyValuePair<string, Func<string, bool>> BlobEndpointSetting = CloudStorageAccount.Setting("BlobEndpoint", new Func<string, bool>(CloudStorageAccount.IsValidUri));
    private static readonly KeyValuePair<string, Func<string, bool>> QueueEndpointSetting = CloudStorageAccount.Setting("QueueEndpoint", new Func<string, bool>(CloudStorageAccount.IsValidUri));
    private static readonly KeyValuePair<string, Func<string, bool>> TableEndpointSetting = CloudStorageAccount.Setting("TableEndpoint", new Func<string, bool>(CloudStorageAccount.IsValidUri));
    private static readonly KeyValuePair<string, Func<string, bool>> FileEndpointSetting = CloudStorageAccount.Setting("FileEndpoint", new Func<string, bool>(CloudStorageAccount.IsValidUri));
    private static readonly KeyValuePair<string, Func<string, bool>> EndpointSuffixSetting = CloudStorageAccount.Setting("EndpointSuffix", new Func<string, bool>(CloudStorageAccount.IsValidDomain));
    private static readonly KeyValuePair<string, Func<string, bool>> SharedAccessSignatureSetting = CloudStorageAccount.Setting("SharedAccessSignature");
    internal const string UseDevelopmentStorageSettingString = "UseDevelopmentStorage";
    internal const string DevelopmentStorageProxyUriSettingString = "DevelopmentStorageProxyUri";
    internal const string DefaultEndpointsProtocolSettingString = "DefaultEndpointsProtocol";
    internal const string AccountNameSettingString = "AccountName";
    internal const string AccountKeyNameSettingString = "AccountKeyName";
    internal const string AccountKeySettingString = "AccountKey";
    internal const string BlobEndpointSettingString = "BlobEndpoint";
    internal const string QueueEndpointSettingString = "QueueEndpoint";
    internal const string TableEndpointSettingString = "TableEndpoint";
    internal const string FileEndpointSettingString = "FileEndpoint";
    internal const string EndpointSuffixSettingString = "EndpointSuffix";
    internal const string SharedAccessSignatureSettingString = "SharedAccessSignature";
    private const string DevstoreAccountName = "devstoreaccount1";
    private const string DevstoreAccountKey = "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw==";
    internal const string SecondaryLocationAccountSuffix = "-secondary";
    private const string DefaultEndpointSuffix = "core.windows.net";
    private const string DefaultBlobHostnamePrefix = "blob";
    private const string DefaultQueueHostnamePrefix = "queue";
    private const string DefaultTableHostnamePrefix = "table";
    private const string DefaultFileHostnamePrefix = "file";

    public static CloudStorageAccount DevelopmentStorageAccount
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    private bool IsDevStoreAccount
    {
        get; set;
    }

    private string EndpointSuffix
    {
        get; set;
    }

    private IDictionary<string, string> Settings
    {
        get; set;
    }

    private bool DefaultEndpoints
    {
        get; set;
    }

    public Uri BlobEndpoint
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public Uri QueueEndpoint
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public Uri TableEndpoint
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public Uri FileEndpoint
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    public StorageUri BlobStorageUri
    {
        get; private set;
    }

    public StorageUri QueueStorageUri
    {
        get; private set;
    }

    public StorageUri TableStorageUri
    {
        get; private set;
    }

    public StorageUri FileStorageUri
    {
        get; private set;
    }

    public StorageCredentials Credentials
    {
        get; private set;
    }

    public CloudStorageAccount(StorageCredentials storageCredentials, Uri blobEndpoint, Uri queueEndpoint, Uri tableEndpoint, Uri fileEndpoint)
      : this(storageCredentials, new StorageUri(blobEndpoint), new StorageUri(queueEndpoint), new StorageUri(tableEndpoint), new StorageUri(fileEndpoint))
    {
        throw new System.NotImplementedException();
    }
	
	//TODO: Make available in RT code
    public CloudStorageAccount(StorageCredentials storageCredentials, StorageUri blobStorageUri, StorageUri queueStorageUri, StorageUri tableStorageUri, StorageUri fileStorageUri)

    {
        throw new System.NotImplementedException();
    }
        public CloudStorageAccount(StorageCredentials storageCredentials, bool useHttps)
      : this(storageCredentials, (string) null, useHttps)
    {
        throw new System.NotImplementedException();
    }
    public CloudStorageAccount(StorageCredentials storageCredentials, string endpointSuffix, bool useHttps)
      : this(storageCredentials, storageCredentials == null ? (string) null : storageCredentials.AccountName, endpointSuffix, useHttps)
    {
        throw new System.NotImplementedException();
    }
    public CloudStorageAccount(StorageCredentials storageCredentials, string accountName, string endpointSuffix, bool useHttps)
    {
        throw new System.NotImplementedException();
    }
    public static CloudStorageAccount Parse(string connectionString)
    {
        throw new System.NotImplementedException();
    }
    public static bool TryParse(string connectionString, out CloudStorageAccount account)
    {
        throw new System.NotImplementedException();
    }
    public CloudTableClient CreateCloudTableClient()
    {
        throw new System.NotImplementedException();
    }
    public CloudQueueClient CreateCloudQueueClient()
    {
        throw new System.NotImplementedException();
    }
    public CloudBlobClient CreateCloudBlobClient()
    {
        throw new System.NotImplementedException();
    }
    public CloudFileClient CreateCloudFileClient()
    {
        throw new System.NotImplementedException();
    }
    public string GetSharedAccessSignature(SharedAccessAccountPolicy policy)
    {
        throw new System.NotImplementedException();
    }
    public override string ToString()
    {
        throw new System.NotImplementedException();
    }
    public string ToString(bool exportSecrets)
    {
        throw new System.NotImplementedException();
    }
    private static CloudStorageAccount GetDevelopmentStorageAccount(Uri proxyUri)
    {
        throw new System.NotImplementedException();
    }
    internal static bool ParseImpl(string connectionString, out CloudStorageAccount accountInformation, Action<string> error)
    {
        throw new System.NotImplementedException();
    }
    private static IDictionary<string, string> ParseStringIntoSettings(string connectionString, Action<string> error)
    {
        throw new System.NotImplementedException();
    }
    private static KeyValuePair<string, Func<string, bool>> Setting(string name, params string[] validValues)
    {
        throw new System.NotImplementedException();
    }
    private static KeyValuePair<string, Func<string, bool>> Setting(string name, Func<string, bool> isValid)
    {
        throw new System.NotImplementedException();
    }
    private static bool IsValidBase64String(string settingValue)
    {
        throw new System.NotImplementedException();
    }
    private static bool IsValidUri(string settingValue)
    {
        throw new System.NotImplementedException();
    }
    private static bool IsValidDomain(string settingValue)
    {
        throw new System.NotImplementedException();
    }
    private static Func<IDictionary<string, string>, IDictionary<string, string>> AllRequired(params KeyValuePair<string, Func<string, bool>>[] requiredSettings)
    {
        throw new System.NotImplementedException();
    }
    private static Func<IDictionary<string, string>, IDictionary<string, string>> Optional(params KeyValuePair<string, Func<string, bool>>[] optionalSettings)
    {
        throw new System.NotImplementedException();
    }
    private static Func<IDictionary<string, string>, IDictionary<string, string>> AtLeastOne(params KeyValuePair<string, Func<string, bool>>[] atLeastOneSettings)
    {
        throw new System.NotImplementedException();
    }
    private static IDictionary<string, string> ValidCredentials(IDictionary<string, string> settings)
    {
        throw new System.NotImplementedException();
    }
    private static bool MatchesSpecification(IDictionary<string, string> settings, params Func<IDictionary<string, string>, IDictionary<string, string>>[] constraints)
    {
        throw new System.NotImplementedException();
    }
    private static StorageCredentials GetCredentials(IDictionary<string, string> settings)
    {
        throw new System.NotImplementedException();
    }
    private static StorageUri ConstructBlobEndpoint(IDictionary<string, string> settings)
    {
        throw new System.NotImplementedException();
    }
    private static StorageUri ConstructBlobEndpoint(string scheme, string accountName, string endpointSuffix)
    {
        throw new System.NotImplementedException();
    }
    private static StorageUri ConstructFileEndpoint(IDictionary<string, string> settings)
    {
        throw new System.NotImplementedException();
    }
    private static StorageUri ConstructFileEndpoint(string scheme, string accountName, string endpointSuffix)
    {
        throw new System.NotImplementedException();
    }
    private static StorageUri ConstructQueueEndpoint(IDictionary<string, string> settings)
    {
        throw new System.NotImplementedException();
    }
    private static StorageUri ConstructQueueEndpoint(string scheme, string accountName, string endpointSuffix)
    {
        throw new System.NotImplementedException();
    }
    private static StorageUri ConstructTableEndpoint(IDictionary<string, string> settings)
    {
        throw new System.NotImplementedException();
    }
    private static StorageUri ConstructTableEndpoint(string scheme, string accountName, string endpointSuffix)
    {
        throw new System.NotImplementedException();
    }
}

}