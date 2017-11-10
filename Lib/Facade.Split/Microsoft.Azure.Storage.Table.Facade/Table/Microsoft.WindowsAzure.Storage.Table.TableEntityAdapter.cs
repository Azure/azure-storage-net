using System.Collections.Generic;
namespace Microsoft.Azure.Storage.Table
{
public class TableEntityAdapter<T> : TableEntity
{
    public T OriginalEntity
    {
        get; set;
    }

    public TableEntityAdapter()
    {
        throw new System.NotImplementedException();
    }
    public TableEntityAdapter(T originalEntity)
    {
        throw new System.NotImplementedException();
    }
    public TableEntityAdapter(T originalEntity, string partitionKey, string rowKey)
      : base(partitionKey, rowKey)
    {
        throw new System.NotImplementedException();
    }
    public override void ReadEntity(IDictionary<string, EntityProperty> properties, OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
    public override IDictionary<string, EntityProperty> WriteEntity(OperationContext operationContext)
    {
        throw new System.NotImplementedException();
    }
}

}