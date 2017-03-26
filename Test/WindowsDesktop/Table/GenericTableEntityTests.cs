using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Table.Entities;

namespace Microsoft.WindowsAzure.Storage.Table
{
    [TestClass]
    public class GenericTableEntityTests : TableTestBase
    {
        [TestMethod]
        [Description("A test to validate that entities stored with the GenericTableEntity type round trip successfully")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableQueryOnGenericEntityRoundTrip()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoTableQueryOnGenericEntityRoundTrip(payloadFormat);
            }
        }

        private void DoTableQueryOnGenericEntityRoundTrip(TablePayloadFormat format)
        {
            CloudTableClient client = GenerateCloudTableClient();

            CloudTable table = client.GetTableReference(GenerateRandomTableName());
            table.Create();
            client.DefaultRequestOptions.PayloadFormat = format;

            Func<GenericEntityPoco, string> generatePartitionKey = (e) => e.State;
            Func<GenericEntityPoco, string> generateRowKey = (e) => $"{e.FirstName}|{e.LastName}";

            try
            {
                TableBatchOperation batch = new TableBatchOperation();

                var entities = new List<GenericEntityPoco>
                {
                    new GenericEntityPoco {State = "KY", FirstName = "Daniel", LastName = "Sparks", Age = 19},
                    new GenericEntityPoco {State = "KY", FirstName = "Fred", LastName = "Turner", Age = 27},
                    new GenericEntityPoco {State = "KY", FirstName = "Mary", LastName = "Rivers", Age = 40},
                };

                foreach (var e in entities)
                {
                    var genericEntity = new GenericTableEntity<GenericEntityPoco>(e, generatePartitionKey(e),
                        generateRowKey(e));
                    batch.Insert(genericEntity);
                }

                table.ExecuteBatch(batch);

                var dataInTable = table.CreateQuery<GenericTableEntity<GenericEntityPoco>>().Execute().OrderBy(e => e.Item.FirstName).ToList();

                for (var i = 0; i < entities.Count; i++)
                {
                    var poco = entities[i];
                    var entity = dataInTable[i];

                    Assert.AreEqual(poco.State, entity.Item.State);
                    Assert.AreEqual(poco.FirstName, entity.Item.FirstName);
                    Assert.AreEqual(poco.LastName, entity.Item.LastName);

                    if (format != TablePayloadFormat.JsonNoMetadata)
                    {
                        //Without the metadata, the type of Age can't be determined,
                        //so it won't deserialize correctly.
                        Assert.AreEqual(poco.Age, entity.Item.Age);
                    }

                    Assert.AreEqual(generatePartitionKey(poco), entity.PartitionKey);
                    Assert.AreEqual(generateRowKey(poco), entity.RowKey);
                }
            }
            finally
            {
                table.DeleteIfExists();
            }
        }
    }
}
