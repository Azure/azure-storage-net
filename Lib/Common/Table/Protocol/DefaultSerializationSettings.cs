using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Microsoft.WindowsAzure.Storage.Table.Protocol
{
    static class DefaultSerializer
    {
        public static JsonSerializer Create()
        {
            return new JsonSerializer
            {
                ContractResolver = new DefaultContractResolver()
            };
        }
    }
    static class DefaultSerializerSettings
    {
        public static JsonSerializerSettings Create()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver()
            };
        }
    }
}