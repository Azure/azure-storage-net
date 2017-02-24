// -----------------------------------------------------------------------------------------
// <copyright file="CloudTableClientTests.cs" company="Microsoft">
//    Copyright 2013 Microsoft Corporation
// 
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
// 
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
// </copyright>
// -----------------------------------------------------------------------------------------

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.WindowsAzure.Storage.Core.Util;
using Microsoft.WindowsAzure.Storage.RetryPolicies;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace Microsoft.WindowsAzure.Storage.Table
{
    /// <summary>
    /// Summary description for CloudTableClientTests
    /// </summary>
    [TestClass]
    public class CloudTableClientTests : TableTestBase
    {
        #region Locals + Ctors
        public CloudTableClientTests()
        {
        }

        private TestContext testContextInstance;

        /// <summary>
        ///Gets or sets the test context which provides
        ///information about and functionality for the current test run.
        ///</summary>
        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        static List<CloudTable> createdTables = new List<CloudTable>();

        #endregion

        #region Additional test attributes
        //
        // You can use the following additional attributes as you write your tests:
        //
        // Use ClassInitialize to run code before running the first test in the class
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            // 20 random tables
            for (int m = 0; m < 20; m++)
            {
                CloudTable tableRef = tableClient.GetTableReference(GenerateRandomTableName());
                tableRef.CreateIfNotExists();
                createdTables.Add(tableRef);
            }

            prefixTablesPrefix = "prefixtable" + GenerateRandomTableName();
            // 20 tables with known prefix
            for (int m = 0; m < 20; m++)
            {
                CloudTable tableRef = tableClient.GetTableReference(prefixTablesPrefix + m.ToString());
                tableRef.CreateIfNotExists();
                createdTables.Add(tableRef);
            }
        }

        private static string prefixTablesPrefix = null;

        // Use ClassCleanup to run code after all tests in a class have run
        [ClassCleanup()]
        public static void MyClassCleanup()
        {
            foreach (CloudTable t in createdTables)
            {
                try
                {
                    t.DeleteIfExists();
                }
                catch (Exception)
                {
                }
            }
        }

        //
        // Use TestInitialize to run code before running each test 
        [TestInitialize()]
        public void MyTestInitialize()
        {
            if (TestBase.TableBufferManager != null)
            {
                TestBase.TableBufferManager.OutstandingBufferCount = 0;
            }
        }
        //
        // Use TestCleanup to run code after each test has run
        [TestCleanup()]
        public void MyTestCleanup()
        {
            if (TestBase.TableBufferManager != null)
            {
                Assert.AreEqual(0, TestBase.TableBufferManager.OutstandingBufferCount);
            }
        }

        #endregion

        #region Ctor Test

        [TestMethod]
        [Description("Test whether we can create a service client with URI and credentials")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableClientConstructor()
        {
            Uri baseAddressUri = new Uri(TestBase.TargetTenantConfig.TableServiceEndpoint);
            CloudTableClient tableClient = new CloudTableClient(baseAddressUri, TestBase.StorageCredentials);
            Assert.IsTrue(tableClient.BaseUri.ToString().Contains(TestBase.TargetTenantConfig.TableServiceEndpoint));
            Assert.AreEqual(TestBase.StorageCredentials, tableClient.Credentials);
            Assert.AreEqual(AuthenticationScheme.SharedKey, tableClient.AuthenticationScheme);

            CloudTableClient tableClient2 = new CloudTableClient(baseAddressUri, null);
            Assert.IsTrue(tableClient2.BaseUri.ToString().Contains(TestBase.TargetTenantConfig.TableServiceEndpoint));
            Assert.AreEqual(AuthenticationScheme.SharedKey, tableClient2.AuthenticationScheme);
        }

        #endregion

        #region List Tables Iterator

        [TestMethod]
        [Description("Test List Tables Iterator No Prefix")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesNoPrefix()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoListTablesNoPrefix(payloadFormat);
            }
        }

        private void DoListTablesNoPrefix(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Check each created table is present
            List<CloudTable> retrievedTables = tableClient.ListTables().ToList();
            foreach (CloudTable t in createdTables)
            {
                Assert.IsNotNull(retrievedTables.Where((tbl) => tbl.Uri == t.Uri).FirstOrDefault());
            }
        }

        [TestMethod]
        [Description("Test List Tables Iterator With Prefix Basic")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesWithPrefixBasic()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoListTablesWithPrefixBasic(payloadFormat);
            }
        }

        private void DoListTablesWithPrefixBasic(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            // Check each created table is present
            List<CloudTable> retrievedTables = tableClient.ListTables(prefixTablesPrefix).ToList();
            foreach (CloudTable t in retrievedTables)
            {
                CloudTable tableref = retrievedTables.Where((tbl) => tbl.Uri == t.Uri).FirstOrDefault();
                Assert.IsNotNull(createdTables.Where((tbl) => tbl.Uri == t.Uri).FirstOrDefault());

                Assert.AreEqual(tableref.Uri, t.Uri);
            }

            Assert.AreEqual(createdTables.Where((tbl) => tbl.Name.StartsWith(prefixTablesPrefix)).Count(), retrievedTables.Count());
        }

        [TestMethod]
        [Description("Test List Tables Iterator With Prefix Extended, will check a variety of ")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesWithPrefixExtended()
        {
            DoListTablesWithPrefixExtended(TablePayloadFormat.Json);
        }

        private void DoListTablesWithPrefixExtended(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            int NumTables = 50;
            int TableNameLength = 8;
            int NumQueries = 100;
            string alpha = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string numerics = "0123456789";
            string legalChars = alpha + numerics;

            string queryString = string.Empty;
            List<CloudTable> tableList = new List<CloudTable>();
            List<CloudTable> localTestCreatedTableList = new List<CloudTable>();

            Random rand = new Random();

            try
            {
                #region Generate Tables

                // Generate Tables in Storage
                // This will generate all caps Tables, i.e. AAAAAAAA, BBBBBBBB....
                for (int h = 26; h < alpha.Length; h++)
                {
                    string tString = string.Empty;
                    for (int i = 0; i < TableNameLength; i++)
                    {
                        tString += alpha[h];
                    }

                    CloudTable table = tableClient.GetTableReference(tString);

                    if (table.CreateIfNotExists())
                    {
                        tableList.Add(table);
                        localTestCreatedTableList.Add(table);
                    }
                }

                // Generate some random tables of TableNameLength, table must start with a letter
                for (int m = 0; m < NumTables; m++)
                {
                    string tableName = GenerateRandomStringFromCharset(1, alpha, rand).ToLower() +
                        GenerateRandomStringFromCharset(TableNameLength - 1, legalChars, rand).ToLower();

                    CloudTable table = tableClient.GetTableReference(tableName);

                    if (table.CreateIfNotExists())
                    {
                        tableList.Add(table);
                        localTestCreatedTableList.Add(table);
                    }
                }

                #endregion

                #region Generate Query Strings to cover all boundary conditions
                List<string> queryStrings = new List<string>() { String.Empty, "aa", "zz", "az", "Az", "Aa", "zZ", "AA", "ZZ", "AZ", "z9", "a9", "aaa" };
                for (int k = 0; k < legalChars.Length; k++)
                {
                    queryStrings.Add(legalChars[k].ToString());
                }

                for (int n = 0; n <= NumQueries; n++)
                {
                    queryStrings.Add(GenerateRandomStringFromCharset((n % TableNameLength) + 1, legalChars, rand));
                }
                #endregion

                #region Merge Created Tables With Pre-existing ones
                int totalTables = 0;
                foreach (CloudTable listedTable in tableClient.ListTables())
                {
                    totalTables++;
                    if (tableList.Where((tbl) => tbl.Uri == listedTable.Uri).FirstOrDefault() != null)
                    {
                        continue;
                    }

                    tableList.Add(listedTable);
                }

                Assert.AreEqual(tableList.Count, totalTables);
                #endregion

                List<CloudTable> serviceResult = null;
                List<CloudTable> LINQResult = null;

                try
                {
                    foreach (string queryValue in queryStrings)
                    {
                        queryString = queryValue;

                        serviceResult = tableClient.ListTables(queryString).OrderBy((table) => table.Name).ToList();
                        LINQResult = tableList.Where((table) => table.Name.ToLower().StartsWith(queryString.ToLower())).OrderBy((table) => table.Name).ToList();

                        Assert.AreEqual(serviceResult.Count(), LINQResult.Count());

                        for (int listDex = 0; listDex < serviceResult.Count(); listDex++)
                        {
                            Assert.AreEqual(serviceResult[listDex].Name, LINQResult[listDex].Name);
                        }
                    }
                }
                catch (Exception)
                {
                    // On exception log table names for repro
                    this.testContextInstance.WriteLine("Exception in ListTablesWithPrefix, Dumping Tables for repro. QueryString = {0}\r\n", queryString);

                    foreach (CloudTable table in tableList)
                    {
                        this.testContextInstance.WriteLine(table.Name);
                    }

                    this.testContextInstance.WriteLine("Linq results =======================");

                    foreach (CloudTable table in LINQResult)
                    {
                        this.testContextInstance.WriteLine(table.Name);
                    }

                    this.testContextInstance.WriteLine("Service results =======================");

                    foreach (CloudTable table in serviceResult)
                    {
                        this.testContextInstance.WriteLine(table.Name);
                    }
                    throw;
                }
            }
            finally
            {
                // Cleanup
                foreach (CloudTable table in localTestCreatedTableList)
                {
                    // Dont delete Class level tables
                    if (createdTables.Where((tbl) => tbl.Uri == table.Uri).FirstOrDefault() != null)
                    {
                        continue;
                    }

                    // Delete other tables
                    table.DeleteIfExists();
                }
            }
        }

        [TestMethod]
        [Description("Test List Tables Iterator using Shared Key Lite")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableClientListTablesSharedKeyLite()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoCloudTableClientListTablesSharedKeyLite(payloadFormat);
            }
        }

        private void DoCloudTableClientListTablesSharedKeyLite(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.AuthenticationScheme = AuthenticationScheme.SharedKeyLite;
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            IEnumerable<CloudTable> actual = tableClient.ListTables();
            Assert.IsNotNull(actual);

            List<CloudTable> retrievedTables = actual.ToList();
            Assert.IsTrue(retrievedTables.Count >= createdTables.Count);

            foreach (CloudTable createdTable in createdTables)
            {
                Assert.IsNotNull(retrievedTables.Where((t) => t.Uri == createdTable.Uri).FirstOrDefault());
            }
        }
        #endregion

        #region List Tables Segmented

        #region Sync

        [TestMethod]
        [Description("Verify WriteXml/ReadXml Serialize/Deserialize on TableContinuationToken")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableContinuationTokenVerifySerializer()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(TableContinuationToken));

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;

            StringReader reader;
            string tokenxml;

            TableContinuationToken writeToken = new TableContinuationToken
            {
                NextPartitionKey = Guid.NewGuid().ToString(),
                NextRowKey = Guid.NewGuid().ToString(),
                NextTableName = Guid.NewGuid().ToString(),
                TargetLocation = StorageLocation.Primary
            };

            TableContinuationToken readToken = null;

            // Write with XmlSerializer
            using (StringWriter writer = new StringWriter())
            {
                serializer.Serialize(writer, writeToken);
                tokenxml = writer.ToString();
            }

            // Read with XmlSerializer
            reader = new StringReader(tokenxml);
            readToken = (TableContinuationToken)serializer.Deserialize(reader);
            Assert.AreEqual(writeToken.NextTableName, readToken.NextTableName);

            // Read with token.ReadXml()
            using (XmlReader xmlReader = XmlReader.Create(new StringReader(tokenxml)))
            {
                readToken = new TableContinuationToken();
                readToken.ReadXml(xmlReader);
            }
            Assert.AreEqual(writeToken.NextTableName, readToken.NextTableName);

            // Read with token.ReadXml()
            using (XmlReader xmlReader = XmlReader.Create(new StringReader(tokenxml)))
            {
                readToken = new TableContinuationToken();
                readToken.ReadXml(xmlReader);
            }

            // Write with token.WriteXml
            StringBuilder sb = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(sb, settings))
            {
                writeToken.WriteXml(writer);
            }

            // Read with XmlSerializer
            reader = new StringReader(sb.ToString());
            readToken = (TableContinuationToken)serializer.Deserialize(reader);
            Assert.AreEqual(writeToken.NextTableName, readToken.NextTableName);

            // Read with token.ReadXml()
            using (XmlReader xmlReader = XmlReader.Create(new StringReader(sb.ToString())))
            {
                readToken = new TableContinuationToken();
                readToken.ReadXml(xmlReader);
            }
            Assert.AreEqual(writeToken.NextTableName, readToken.NextTableName);
        }

        [TestMethod]
        [Description("Verify ReadXml Deserialization on TableContinuationToken with empty TargetLocation")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableContinuationTokenVerifyEmptyTargetDeserializer()
        {
            TableContinuationToken tableContinuationToken = new TableContinuationToken { NextTableName = "tableName", NextPartitionKey = "partitionKey", NextRowKey = "rowKey", TargetLocation = null };
            StringBuilder stringBuilder = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(stringBuilder))
            {
                tableContinuationToken.WriteXml(writer);
            }

            string stringToken = stringBuilder.ToString();
            TableContinuationToken parsedToken = new TableContinuationToken();
            parsedToken.ReadXml(XmlReader.Create(new System.IO.StringReader(stringToken)));
            Assert.AreEqual(parsedToken.NextTableName, "tableName");
            Assert.AreEqual(parsedToken.NextPartitionKey, "partitionKey");
            Assert.AreEqual(parsedToken.NextRowKey, "rowKey");
            Assert.AreEqual(parsedToken.TargetLocation, null);
        }

        [TestMethod]
        [Description("Verify GetSchema, WriteXml and ReadXml on TableContinuationToken")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableContinuationTokenVerifyXmlFunctions()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            TableResultSegment segment = null;
            List<CloudTable> totalResults = new List<CloudTable>();
            TableContinuationToken token = null;
            do
            {
                segment = tableClient.ListTablesSegmented("prefixtable", 5, segment != null ? segment.ContinuationToken : null);
                totalResults.AddRange(segment);
                token = segment.ContinuationToken;
                if (token != null)
                {
                    Assert.AreEqual(null, token.GetSchema());

                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    StringBuilder sb = new StringBuilder();
                    using (XmlWriter writer = XmlWriter.Create(sb, settings))
                    {
                        token.WriteXml(writer);
                    }

                    using (XmlReader reader = XmlReader.Create(new StringReader(sb.ToString())))
                    {
                        token = new TableContinuationToken();
                        token.ReadXml(reader);
                    }
                }
            }
            while (token != null);

            Assert.AreEqual(totalResults.Count, tableClient.ListTables("prefixtable").Count());
        }

        [TestMethod]
        [Description("Verify WriteXml and ReadXml on TableContinuationToken within another XML")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void TableContinuationTokenVerifyXmlWithinXml()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            TableResultSegment segment = null;
            List<CloudTable> totalResults = new List<CloudTable>();
            TableContinuationToken token = null;
            do
            {
                segment = tableClient.ListTablesSegmented("prefixtable", 5, segment != null ? segment.ContinuationToken : null);
                totalResults.AddRange(segment);
                token = segment.ContinuationToken;
                if (token != null)
                {
                    Assert.AreEqual(null, token.GetSchema());

                    XmlWriterSettings settings = new XmlWriterSettings();
                    settings.Indent = true;
                    StringBuilder sb = new StringBuilder();
                    using (XmlWriter writer = XmlWriter.Create(sb, settings))
                    {
                        writer.WriteStartElement("test1");
                        writer.WriteStartElement("test2");
                        token.WriteXml(writer);
                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }

                    using (XmlReader reader = XmlReader.Create(new StringReader(sb.ToString())))
                    {
                        token = new TableContinuationToken();
                        reader.ReadStartElement();
                        reader.ReadStartElement();
                        token.ReadXml(reader);
                        reader.ReadEndElement();
                        reader.ReadEndElement();
                    }
                }
            }
            while (token != null);

            Assert.AreEqual(totalResults.Count, tableClient.ListTables("prefixtable").Count());
        }

        [TestMethod]
        [Description("Test List Tables Segmented Basic Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesSegmentedBasicSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoListTablesSegmentedBasicSync(payloadFormat);
            }
        }

        private void DoListTablesSegmentedBasicSync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableResultSegment segment = null;
            List<CloudTable> totalResults = new List<CloudTable>();

            do
            {
                segment = tableClient.ListTablesSegmented(segment != null ? segment.ContinuationToken : null);
                totalResults.AddRange(segment);
            }
            while (segment.ContinuationToken != null);

            Assert.AreEqual(totalResults.Count, tableClient.ListTables().Count());
        }

        [TestMethod]
        [Description("Test List Tables Segmented MaxResults Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesSegmentedMaxResultsSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoListTablesSegmentedMaxResultsSync(payloadFormat);
            }
        }

        private void DoListTablesSegmentedMaxResultsSync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableResultSegment segment = null;
            List<CloudTable> totalResults = new List<CloudTable>();

            int segCount = 0;
            do
            {
                segment = tableClient.ListTablesSegmented(string.Empty, 10, segment != null ? segment.ContinuationToken : null, null, null);
                totalResults.AddRange(segment);
                segCount++;
            }
            while (segment.ContinuationToken != null);

            Assert.AreEqual(totalResults.Count, tableClient.ListTables().Count());
            Assert.IsTrue(segCount >= totalResults.Count / 10);
        }

        [TestMethod]
        [Description("Test List Tables Segmented With Prefix Sync")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesSegmentedWithPrefixSync()
        {
            foreach (TablePayloadFormat payloadFormat in Enum.GetValues(typeof(TablePayloadFormat)))
            {
                DoListTablesSegmentedWithPrefixSync(payloadFormat);
            }
        }

        private void DoListTablesSegmentedWithPrefixSync(TablePayloadFormat format)
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            tableClient.DefaultRequestOptions.PayloadFormat = format;

            TableResultSegment segment = null;
            List<CloudTable> totalResults = new List<CloudTable>();

            int segCount = 0;
            do
            {
                segment = tableClient.ListTablesSegmented(prefixTablesPrefix, null, segment != null ? segment.ContinuationToken : null, null, null);
                totalResults.AddRange(segment);
                segCount++;
            }
            while (segment.ContinuationToken != null);

            Assert.AreEqual(totalResults.Count, 20);
            foreach (CloudTable tbl in totalResults)
            {
                Assert.IsTrue(tbl.Name.StartsWith(prefixTablesPrefix));
            }
        }
        #endregion

        #region APM

        [TestMethod]
        [Description("Test List Tables Segmented Basic APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesSegmentedBasicAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            TableResultSegment segment = null;
            List<CloudTable> totalResults = new List<CloudTable>();

            do
            {
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult asyncRes = tableClient.BeginListTablesSegmented(segment != null ? segment.ContinuationToken : null,
                        (res) =>
                        {
                            evt.Set();
                        },
                        null);

                    evt.WaitOne();

                    segment = tableClient.EndListTablesSegmented(asyncRes);
                }

                totalResults.AddRange(segment);
            }
            while (segment.ContinuationToken != null);

            Assert.AreEqual(totalResults.Count, tableClient.ListTables().Count());
        }

        [TestMethod]
        [Description("Test List Tables Segmented MaxResults APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesSegmentedMaxResultsAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            TableResultSegment segment = null;
            List<CloudTable> totalResults = new List<CloudTable>();

            int segCount = 0;
            do
            {
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult asyncRes = tableClient.BeginListTablesSegmented(string.Empty,
                        10,
                        segment != null ? segment.ContinuationToken : null,
                        null,
                        null,
                        (res) =>
                        {
                            evt.Set();
                        },
                        null);

                    evt.WaitOne();

                    segment = tableClient.EndListTablesSegmented(asyncRes);
                }

                Assert.IsTrue(segment.Count() <= 10);

                totalResults.AddRange(segment);
                segCount++;
            }
            while (segment.ContinuationToken != null);

            Assert.AreEqual(totalResults.Count, tableClient.ListTables().Count());
            Assert.IsTrue(segCount >= totalResults.Count / 10);
        }

        [TestMethod]
        [Description("Test List Tables Segmented With Prefix APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesSegmentedWithPrefixAPM()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();

            TableResultSegment segment = null;
            List<CloudTable> totalResults = new List<CloudTable>();

            int segCount = 0;
            do
            {
                using (ManualResetEvent evt = new ManualResetEvent(false))
                {
                    IAsyncResult asyncRes = tableClient.BeginListTablesSegmented(prefixTablesPrefix,
                        null,
                        segment != null ? segment.ContinuationToken : null,
                        null,
                        null,
                        (res) =>
                        {
                            evt.Set();
                        },
                        null);

                    evt.WaitOne();

                    segment = tableClient.EndListTablesSegmented(asyncRes);
                }

                totalResults.AddRange(segment);
                segCount++;
            }
            while (segment.ContinuationToken != null);

            Assert.AreEqual(totalResults.Count, 20);
            foreach (CloudTable tbl in totalResults)
            {
                Assert.IsTrue(tbl.Name.StartsWith(prefixTablesPrefix));
            }
        }

        #endregion

        #region Task

#if TASK
        [TestMethod]
        [Description("Test TableClient ListTablesSegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesSegmentedTokenTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableContinuationToken token = null;

            do
            {
                TableResultSegment resultSegment = tableClient.ListTablesSegmentedAsync(token).Result;
                token = resultSegment.ContinuationToken;
            }
            while (token != null);
        }

        [TestMethod]
        [Description("Test TableClient ListTablesSegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesSegmentedPrefixTokenTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string prefix = prefixTablesPrefix;
            TableContinuationToken token = null;

            int totalCount = 0;
            do
            {
                TableResultSegment resultSegment = tableClient.ListTablesSegmentedAsync(prefix, token).Result;
                token = resultSegment.ContinuationToken;

                foreach (CloudTable table in resultSegment)
                {
                    Assert.IsTrue(table.Name.StartsWith(prefix));
                    ++totalCount;
                }
            }
            while (token != null);

            Assert.AreEqual(20, totalCount);
        }

        [TestMethod]
        [Description("Test TableClient ListTablesSegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesSegmentedTokenCancellationTokenTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            TableContinuationToken token = null;
            CancellationToken cancellationToken = CancellationToken.None;

            do
            {
                TableResultSegment resultSegment = tableClient.ListTablesSegmentedAsync(token, cancellationToken).Result;
                token = resultSegment.ContinuationToken;
            }
            while (token != null);
        }

        [TestMethod]
        [Description("Test TableClient ListTablesSegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesSegmentedPrefixTokenCancellationTokenTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string prefix = prefixTablesPrefix;
            TableContinuationToken token = null;
            CancellationToken cancellationToken = CancellationToken.None;

            int totalCount = 0;
            do
            {
                TableResultSegment resultSegment = tableClient.ListTablesSegmentedAsync(prefix, token, cancellationToken).Result;
                token = resultSegment.ContinuationToken;

                foreach (CloudTable table in resultSegment)
                {
                    Assert.IsTrue(table.Name.StartsWith(prefix));
                    ++totalCount;
                }
            }
            while (token != null);

            Assert.AreEqual(20, totalCount);
        }

        [TestMethod]
        [Description("Test TableClient ListTablesSegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesSegmentedPrefixMaxResultsTokenRequestOptionsOperationContextTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string prefix = prefixTablesPrefix;
            int? maxResults = 10;
            TableContinuationToken token = null;
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();

            int totalCount = 0;
            do
            {
                TableResultSegment resultSegment = tableClient.ListTablesSegmentedAsync(prefix, maxResults, token, requestOptions, operationContext).Result;
                token = resultSegment.ContinuationToken;

                int count = 0;
                foreach (CloudTable table in resultSegment)
                {
                    Assert.IsTrue(table.Name.StartsWith(prefix));
                    ++count;
                }

                totalCount += count;

                Assert.IsTrue(count <= maxResults.Value);
            }
            while (token != null);

            Assert.AreEqual(20, totalCount);
        }

        [TestMethod]
        [Description("Test TableClient ListTablesSegmented - Task")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void ListTablesSegmentedPrefixMaxResultsTokenRequestOptionsOperationContextCancellationTokenTask()
        {
            CloudTableClient tableClient = GenerateCloudTableClient();
            string prefix = prefixTablesPrefix;
            int? maxResults = 10;
            TableContinuationToken token = null;
            TableRequestOptions requestOptions = new TableRequestOptions();
            OperationContext operationContext = new OperationContext();
            CancellationToken cancellationToken = CancellationToken.None;

            int totalCount = 0;
            do
            {
                TableResultSegment resultSegment = tableClient.ListTablesSegmentedAsync(prefix, maxResults, token, requestOptions, operationContext, cancellationToken).Result;
                token = resultSegment.ContinuationToken;

                int count = 0;
                foreach (CloudTable table in resultSegment)
                {
                    Assert.IsTrue(table.Name.StartsWith(prefix));
                    ++count;
                }

                totalCount += count;

                Assert.IsTrue(count <= maxResults.Value);
            }
            while (token != null);

            Assert.AreEqual(20, totalCount);
        }
#endif

        #endregion

        #endregion

        [TestMethod]
        [Description("Get service stats")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableClientGetServiceStats()
        {
            AssertSecondaryEndpoint();

            CloudTableClient client = GenerateCloudTableClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.SecondaryOnly;
            TestHelper.VerifyServiceStats(client.GetServiceStats());
        }

        [TestMethod]
        [Description("Get service stats")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableClientGetServiceStatsAPM()
        {
            AssertSecondaryEndpoint();

            CloudTableClient client = GenerateCloudTableClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.SecondaryOnly;
            using (AutoResetEvent waitHandle = new AutoResetEvent(false))
            {
                IAsyncResult result = client.BeginGetServiceStats(
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.VerifyServiceStats(client.EndGetServiceStats(result));

                result = client.BeginGetServiceStats(
                    null,
                    new OperationContext(),
                    ar => waitHandle.Set(),
                    null);
                waitHandle.WaitOne();
                TestHelper.VerifyServiceStats(client.EndGetServiceStats(result));
            }
        }

#if TASK
        [TestMethod]
        [Description("Get service stats")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableClientGetServiceStatsAsync()
        {
            AssertSecondaryEndpoint();

            CloudTableClient client = GenerateCloudTableClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.SecondaryOnly;
            TestHelper.VerifyServiceStats(client.GetServiceStatsAsync().Result);
        }
#endif

        [TestMethod]
        [Description("Testing GetServiceStats with invalid Location Mode - SYNC")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableClientGetServiceStatsInvalidLoc()
        {
            CloudTableClient client = GenerateCloudTableClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.PrimaryOnly;
            try
            {
                client.GetServiceStats();
                Assert.Fail("GetServiceStats should fail and throw an InvalidOperationException.");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(InvalidOperationException));
            }
        }

        [TestMethod]
        [Description("Testing GetServiceStats with invalid Location Mode - APM")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableClientGetServiceStatsInvalidLocAPM()
        {
            CloudTableClient client = GenerateCloudTableClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.PrimaryOnly;
            try
            {
                using (AutoResetEvent waitHandle = new AutoResetEvent(false))
                {
                    IAsyncResult result = client.BeginGetServiceStats(
                        ar => waitHandle.Set(),
                        null);
                    waitHandle.WaitOne();
                }

                Assert.Fail("GetServiceStats should fail and throw an InvalidOperationException.");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(InvalidOperationException));
            }
        }

        [TestMethod]
        [Description("Testing GetServiceStats with invalid Location Mode - ASYNC")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableClientGetServiceStatsInvalidLocAsync()
        {
            CloudTableClient client = GenerateCloudTableClient();
            client.DefaultRequestOptions.LocationMode = LocationMode.PrimaryOnly;
            try
            {
                client.GetServiceStatsAsync();
                Assert.Fail("GetServiceStats should fail and throw an InvalidOperationException.");
            }
            catch (Exception e)
            {
                Assert.IsInstanceOfType(e, typeof(InvalidOperationException));
            }
        }

        [TestMethod]
        [Description("Server timeout query parameter")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableClientServerTimeout()
        {
            CloudTableClient client = GenerateCloudTableClient();

            string timeout = null;
            OperationContext context = new OperationContext();
            context.SendingRequest += (sender, e) =>
            {
                IDictionary<string, string> query = HttpWebUtility.ParseQueryString(e.Request.RequestUri.Query);
                if (!query.TryGetValue("timeout", out timeout))
                {
                    timeout = null;
                }
            };

            TableRequestOptions options = new TableRequestOptions();
            client.GetServiceProperties(null, context);
            Assert.IsNull(timeout);
            client.GetServiceProperties(options, context);
            Assert.IsNull(timeout);

            options.ServerTimeout = TimeSpan.FromSeconds(100);
            client.GetServiceProperties(options, context);
            Assert.AreEqual("100", timeout);

            client.DefaultRequestOptions.ServerTimeout = TimeSpan.FromSeconds(90);
            client.GetServiceProperties(null, context);
            Assert.AreEqual("90", timeout);
            client.GetServiceProperties(options, context);
            Assert.AreEqual("100", timeout);

            options.ServerTimeout = null;
            client.GetServiceProperties(options, context);
            Assert.AreEqual("90", timeout);

            options.ServerTimeout = TimeSpan.Zero;
            client.GetServiceProperties(options, context);
            Assert.IsNull(timeout);
        }

        [TestMethod]
        [Description("Check for maximum execution time limit")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableClientMaximumExecutionTimeCheck()
        {
            try
            {
                CloudTableClient client = GenerateCloudTableClient();
                client.DefaultRequestOptions.MaximumExecutionTime = TimeSpan.FromDays(25.0);
                Assert.Fail();
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ArgumentOutOfRangeException));
            }
        }

        [TestMethod]
        [Description("Check for null pk/rk")]
        [TestCategory(ComponentCategory.Table)]
        [TestCategory(TestTypeCategory.UnitTest)]
        [TestCategory(SmokeTestCategory.NonSmoke)]
        [TestCategory(TenantTypeCategory.DevStore), TestCategory(TenantTypeCategory.DevFabric), TestCategory(TenantTypeCategory.Cloud)]
        public void CloudTableClientNullPartitionKeyRowKeyCheck()
        {
            CloudTableClient client = GenerateCloudTableClient();
            var table = client.GetTableReference("newtable");
            table.CreateIfNotExists();
            try
            {
                TableBatchOperation batch = new TableBatchOperation();
                TableEntity entity = new TableEntity(null, "foo");
                batch.InsertOrMerge(entity);
                table.ExecuteBatch(batch);
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ArgumentNullException));
            }

            try
            {
                TableBatchOperation batch = new TableBatchOperation();
                TableEntity entity = new TableEntity("foo", null);
                batch.InsertOrMerge(entity);
                table.ExecuteBatch(batch);
            }
            catch (Exception ex)
            {
                Assert.IsInstanceOfType(ex, typeof(ArgumentNullException));
            }
        }
    }
}