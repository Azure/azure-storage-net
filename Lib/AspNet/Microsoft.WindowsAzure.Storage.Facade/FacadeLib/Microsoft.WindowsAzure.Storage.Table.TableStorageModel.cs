using Microsoft.Data.Edm;
using Microsoft.Data.Edm.Library;
using Microsoft.Data.OData;
using Microsoft.WindowsAzure.Storage.Core.Util;
using System;
namespace Microsoft.WindowsAzure.Storage.Table
{
public class TableStorageModel : EdmModel, IEdmModel, IEdmElement
{

    internal string AccountName
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal EdmEntityType TableType
    {
        get
        {
            throw new System.NotImplementedException();
        }
    }

    internal TableStorageModel()
      : this("account")
    {
        throw new System.NotImplementedException();
    }
    public TableStorageModel(string accountName)
    {
        throw new System.NotImplementedException();
    }
    IEdmSchemaType IEdmModel.FindDeclaredType(string qualifiedName)
    {
        throw new System.NotImplementedException();
    }
    internal static EdmEntityType CreateEntityType(string namespaceName, string name)
    {
        throw new System.NotImplementedException();
    }
    internal bool IsKnownType(string qualifiedName)
    {
        throw new System.NotImplementedException();
    }
    private static void SplitFullTypeName(string qualifiedName, out string name, out string namespaceName)
    {
        throw new System.NotImplementedException();
    }
}

}