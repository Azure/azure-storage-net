using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Azure.Storage.RetryPolicies;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;

namespace WindowsFormsTest
{
    public partial class Form1 : Form
    {
        readonly TestConfigurations configurations;
        readonly TenantConfiguration defaultTenantConfiguration;


        private const AuthenticationScheme DefaultAuthenticationScheme = AuthenticationScheme.SharedKey;

        public Form1()
        {
            InitializeComponent();

            try
            {
                XElement element = XElement.Load(TestConfigurations.DefaultTestConfigFilePath);
                this.configurations = TestConfigurations.ReadFromXml(element);

                this.defaultTenantConfiguration = this.configurations.TenantConfigurations.Single(c => c.TenantName == this.configurations.TargetTenantName);
            }
            catch (System.IO.FileNotFoundException)
            {
                throw new System.IO.FileNotFoundException("To run tests you need to supply a TestConfigurations.xml file with credentials in the Test/Common folder. Use TestConfigurationsTemplate.xml as a template.");
            }
        }

        CloudBlobClient GenerateCloudBlobClient(DelegatingHandler delegatingHandler = null)
        {
            var storageCredentials = new StorageCredentials(this.defaultTenantConfiguration.AccountName, this.defaultTenantConfiguration.AccountKey);

            CloudBlobClient client;
            if (string.IsNullOrEmpty(this.defaultTenantConfiguration.BlobServiceSecondaryEndpoint))
            {
                Uri baseAddressUri = new Uri(this.defaultTenantConfiguration.BlobServiceEndpoint);
                client = new CloudBlobClient(baseAddressUri, storageCredentials, delegatingHandler);
            }
            else
            {
                StorageUri baseAddressUri = new StorageUri(
                    new Uri(this.defaultTenantConfiguration.BlobServiceEndpoint),
                    new Uri(this.defaultTenantConfiguration.BlobServiceSecondaryEndpoint));
                client = new CloudBlobClient(baseAddressUri, storageCredentials, delegatingHandler);
            }

            client.AuthenticationScheme = DefaultAuthenticationScheme;

            return client;
        }

        private void BlockUploadFromByteArray_Click(object sender, EventArgs e)
        {
            CloudBlobClient client = this.GenerateCloudBlobClient();
            CloudBlobContainer container = client.GetContainerReference("windows-forms-upload-test-" + Guid.NewGuid().ToString());
            container.CreateIfNotExists();
            TimeSpan backoffPeriod = new TimeSpan();
            try
            {
                BlobRequestOptions options = new BlobRequestOptions()
                {
                    SingleBlobUploadThresholdInBytes = 5 * 1024 * 1024,
                    ParallelOperationThreadCount = 5,
                    RetryPolicy = new ExponentialRetry(backoffPeriod, 2)
                };
                var bytes = new byte[40 * 1024 * 1024];
                var cloudBlockBlob = container.GetBlockBlobReference("data.bin");
                cloudBlockBlob.UploadFromByteArray(bytes, 0, bytes.Length, null, options, null);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                throw;
            }
            finally
            {
                container.DeleteIfExists();
            }
        }
    }
}
