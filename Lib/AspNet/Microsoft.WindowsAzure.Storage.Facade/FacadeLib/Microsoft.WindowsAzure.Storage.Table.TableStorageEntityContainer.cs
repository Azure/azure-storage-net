using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;
namespace Microsoft.WindowsAzure.Storage.Table
{
internal class TableStorageEntityContainer : EdmEntityContainer
{

    public TableStorageEntityContainer(TableStorageModel model)
            : base("AzureTableStorage", "DefaultContainer")
    {
        throw new System.NotImplementedException();
    }

    private string InferServerTypeNameFromTableName(string setName)
    {
        throw new System.NotImplementedException();
    }
}

}