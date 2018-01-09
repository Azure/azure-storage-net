using Microsoft.WindowsAzure.Storage.Shared.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
#if NETCORE
using System.Threading;
#else
using System.Threading.Tasks;
#endif

namespace Microsoft.WindowsAzure.Storage.Blob
{
    public static class TestExtensions
    {
        internal static void EnableSoftDelete(this CloudBlobClient client)
        {
            ServiceProperties props = new ServiceProperties(
                new LoggingProperties(),
                new MetricsProperties(),
                new MetricsProperties(),
                new CorsProperties(),
                new DeleteRetentionPolicy()
                {
                    Enabled = true,
                    RetentionDays = 1
                }
            );

            client.SetServicePropertiesAsync(props).Wait();
#if NETCORE
            Thread.Sleep(30000);
#else
            Task.Delay(TimeSpan.FromSeconds(30)).Wait();
#endif
        }

        internal static void DisableSoftDelete(this CloudBlobClient client)
        {
            ServiceProperties props = new ServiceProperties(
                deleteRetentionPolicy:
                new DeleteRetentionPolicy()
                {
                    Enabled = false
                }
            );

            client.SetServicePropertiesAsync(props).Wait();
        }
    }
}
